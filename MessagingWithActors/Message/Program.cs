using System;
using Akka.Actor;

namespace Message
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var primitiveActor = system.ActorOf<ScalarValuePrinter>("scalarValuePrinter");
                primitiveActor.Tell(100);
                primitiveActor.Tell("Hello");

                var orderProcessor = system.ActorOf<OrderProcessor>("orderProcessor");
                system.ActorOf(Props.Create(() => new Caller(orderProcessor)),"caller");
            }
            Console.ReadLine();
        }
    }
    public class ScalarValuePrinter : ReceiveActor
    {
        public ScalarValuePrinter()
        {
            Receive<string>(x => Console.WriteLine($"ScalarValuePrinter: received String {x}"));
            Receive<int>(x => Console.WriteLine($"ScalarValuePrinter: received Int32 {x}"));
        }
    }

    public class OrderProcessor : ReceiveActor
    {
        public OrderProcessor()
        {
            Receive<ExecuteBuyOrder>(x => Sender.Tell(new BuyOrderExecuted()));
            Receive<ExecuteSellOrder>(x => Sender.Tell(new SellOrderExecuted()));
        }
    }

    public class Caller : ReceiveActor
    {
        public Caller(IActorRef orderProcessor)
        {
            Receive<BuyOrderExecuted>(x => Console.WriteLine("Caller: received BuyOrderExecuted"));
            Receive<SellOrderExecuted>(x => Console.WriteLine("Caller: received SellOrderExecuted"));

            orderProcessor.Tell(new ExecuteBuyOrder());
            orderProcessor.Tell(new ExecuteSellOrder());
        }
    }

    public class OrderProcessorCommand { }
    public class ExecuteBuyOrder: OrderProcessorCommand { }
    public class ExecuteSellOrder: OrderProcessorCommand { }

    public class OrderProcessorEvent { }
    public class BuyOrderExecuted : OrderProcessorEvent { }
    public class SellOrderExecuted : OrderProcessorEvent { }
}
