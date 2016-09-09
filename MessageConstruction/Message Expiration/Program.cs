using System;
using Akka.Actor;

namespace Message_Expiration
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var purchaseAgent = system.ActorOf<PurchaseAgent>("purchaseAgent");
                var purchaseRouter = system.ActorOf(Props.Create(() => new PurchaseRouter(purchaseAgent)), "purchaseRouter");

                var start = purchaseAgent.Ask(new Start());

                purchaseRouter.Tell(new PlaceOrder(DateTime.Now.AddDays(-1), DateTime.Now));
                purchaseRouter.Tell(new PlaceOrder(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1)));
                purchaseRouter.Tell(new PlaceOrder(DateTime.Now.AddDays(-1), DateTime.Now.AddMilliseconds(1)));
            }
            Console.ReadLine();
        }
    }

    public class PurchaseRouter : ReceiveActor
    {
        public PurchaseRouter(IActorRef purchaseAgent)
        {
            Receive<PlaceOrder>(x =>
            {
                var mili = new Random().Next(100) + 1;
                Console.WriteLine($"PurchaseRouter: delaying delivery of {x} for {mili} milliseconds");
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(mili), purchaseAgent, x, Self);
            });
        }
    }

    public class PurchaseAgent : ReceiveActor
    {
        public PurchaseAgent()
        {
            Receive<PlaceOrder>(x =>
            {
                if(IsExpired(x))
                {
                    Context.System.DeadLetters.Tell(x);
                    Console.WriteLine($"PurchaseAgent: delivered expired {x} A to dead letters");
                }
                else
                {
                    Console.WriteLine($"PurchaseAgent: placing order for {x}");
                }
            });
            Receive<Start>(x => Sender.Tell("ok"));
        }

        private bool IsExpired(PlaceOrder order) => order.ExpirationDate > DateTime.Now;
    }
    public class Start { }
    public class PlaceOrder {
        public DateTime OccurredOn { get; }
        public DateTime ExpirationDate { get; }
        public PlaceOrder(DateTime occurredOn, DateTime expirationDate)
        {
            OccurredOn = occurredOn;
            ExpirationDate = expirationDate;
        }
    }
}


//Getting started in Domotic apps with Raspberry, nodeJS and websockets