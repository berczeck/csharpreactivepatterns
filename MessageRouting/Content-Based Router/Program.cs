using System;
using Akka.Actor;

namespace Content_Based_Router
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var orderRouter = system.ActorOf<OrderRouter>("orderRouter");
                orderRouter.Tell(new OrderPlaced("TypeABC"));
                orderRouter.Tell(new OrderPlaced("TypeXYZ"));
                Console.ReadLine();
            }
        }
    }

    public class OrderRouter : ReceiveActor
    {
        private IActorRef inventorySystemA;
        private IActorRef inventorySystemX;

        public OrderRouter()
        {
            Receive<OrderPlaced>(x =>
            {
                switch (x.Type)
                {
                    case "TypeABC":
                        Console.WriteLine($"OrderRouter: routing {x.Type} to inventorySystemA");
                        inventorySystemA.Tell(x);
                        break;
                    case "TypeXYZ":
                        Console.WriteLine($"OrderRouter: routing {x.Type} to inventorySystemX");
                        inventorySystemX.Tell(x);
                        break;
                    default:
                        Console.WriteLine("OrderRouter: received unexpected message");
                        break;
                }
            });
        }

        protected override void PreStart()
        {
            inventorySystemA = Context.ActorOf<InventorySystemA>("inventorySystemA");
            inventorySystemX = Context.ActorOf<InventorySystemX>("inventorySystemX");
            base.PreStart();
        }
    }

    public class InventorySystemA : ReceiveActor
    {
        public InventorySystemA()
        {
            Receive<OrderPlaced>(x => Console.WriteLine($"InventorySystemA: handling {x.Type}"));
        }
    }

    public class InventorySystemX : ReceiveActor
    {
        public InventorySystemX()
        {
            Receive<OrderPlaced>(x => Console.WriteLine($"InventorySystemX: handling {x.Type}"));
        }
    }

    public class OrderPlaced
    {
        public string Type { get; }
        public OrderPlaced(string type)
        {
            Type = type;
        }
    }
}
