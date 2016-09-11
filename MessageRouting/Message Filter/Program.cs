using System;
using Akka.Actor;

namespace Message_Filter
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var systemA = system.ActorOf<InventorySystemA>("systemA");
                var systemX = system.ActorOf<InventorySystemX>("systemX");
                var inventorySystemX = system.ActorOf(Props.Create(() =>
                new InventorySystemXMessageFilter(systemX)), "inventorySystemX");

                systemA.Tell(new OrderPlaced("TypeABC"));
                inventorySystemX.Tell(new OrderPlaced("TypeABC"));

                systemA.Tell(new OrderPlaced("TypeXYZ"));
                inventorySystemX.Tell(new OrderPlaced("TypeXYZ"));

                Console.ReadLine();
            }
        }
    }

    public class InventorySystemXMessageFilter : ReceiveActor
    {
        public InventorySystemXMessageFilter(IActorRef inventorySystemX)
        {
            Receive<OrderPlaced>(x =>
            {
                switch (x.Type)
                {
                    case "TypeXYZ":
                        inventorySystemX.Forward(x);
                        break;
                    default:
                        Console.WriteLine($"InventorySystemXMessageFilter: filtering: {x.Type}");
                        break;
                }
            });
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
