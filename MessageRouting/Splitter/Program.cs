using System;
using System.Collections.Generic;
using Akka.Actor;
using System.Linq;

namespace Splitter
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var orderRouter = system.ActorOf<OrderRouter>("orderRouter");

                var orderItem1 = new OrderItem("1", "TypeA", "An item of type A.", 23.95m);
                var orderItem2 = new OrderItem("2", "TypeB", "An item of type B.", 99.95m);
                var orderItem3 = new OrderItem("3", "TypeC", "An item of type C.", 14.95m);
                var orderItems = new List<OrderItem> { orderItem1, orderItem2, orderItem3 };

                orderRouter.Tell(new OrderPlaced(orderItems));

                Console.ReadLine();
            }
        }
    }
    
    public class OrderRouter : ReceiveActor
    {
        private IActorRef orderItemTypeAProcessor;
        private IActorRef orderItemTypeBProcessor;
        private IActorRef orderItemTypeCProcessor;

        public OrderRouter()
        {
            Receive<OrderPlaced>(order =>
            {
                order.OrderItems.ForEach(item =>
                {
                    switch (item.ItemType)
                    {
                        case "TypeA":
                            Console.WriteLine($"OrderRouter: routing {item.ItemType}");
                            orderItemTypeAProcessor.Tell(new TypeAItemOrdered(item.Id, item.ItemType, item.Description, item.Price));
                            break;
                        case "TypeB":
                            Console.WriteLine($"OrderRouter: routing {item.ItemType}");
                            orderItemTypeBProcessor.Tell(new TypeBItemOrdered(item.Id, item.ItemType, item.Description, item.Price));
                            break;
                        case "TypeC":
                            Console.WriteLine($"OrderRouter: routing {item.ItemType}");
                            orderItemTypeCProcessor.Tell(new TypeCItemOrdered(item.Id, item.ItemType, item.Description, item.Price));
                            break;
                        default:
                            break;
                    }
                });
            });
        }

        protected override void PreStart()
        {
            orderItemTypeAProcessor = Context.ActorOf<OrderItemTypeAProcessor>("orderItemTypeAProcessor");
            orderItemTypeBProcessor = Context.ActorOf<OrderItemTypeBProcessor>("orderItemTypeBProcessor");
            orderItemTypeCProcessor = Context.ActorOf<OrderItemTypeCProcessor>("orderItemTypeCProcessor");
            base.PreStart();
        }
    }

    public class OrderItemTypeAProcessor : ReceiveActor
    {
        public OrderItemTypeAProcessor()
        {
            Receive<TypeAItemOrdered>(x => Console.WriteLine($"OrderItemTypeAProcessor: handling {x}"));
        }
    }

    public class OrderItemTypeBProcessor : ReceiveActor
    {
        public OrderItemTypeBProcessor()
        {
            Receive<TypeBItemOrdered>(x => Console.WriteLine($"OrderItemTypeBProcessor: handling {x}"));
        }
    }

    public class OrderItemTypeCProcessor : ReceiveActor
    {
        public OrderItemTypeCProcessor()
        {
            Receive<TypeCItemOrdered>(x => Console.WriteLine($"OrderItemTypeCProcessor: handling {x}"));
        }
    }

    public class Order
    {
        public List<OrderItem> OrderItems { get; }

        public Order(List<OrderItem> orderItems)
        {
            OrderItems = orderItems;
        }
    }

    public class OrderPlaced : Order
    {
        public OrderPlaced(List<OrderItem> orderItems):base(orderItems)
        {

        }
    }
    public class TypeAItemOrdered : OrderItem
    {
        public TypeAItemOrdered(string id, string itemType, string description, decimal price)
            :base(id,itemType,description,price)
        {

        }
    }
    public class TypeBItemOrdered : OrderItem
    {
        public TypeBItemOrdered(string id, string itemType, string description, decimal price)
            : base(id, itemType, description, price)
        {

        }
    }
    public class TypeCItemOrdered : OrderItem
    {
        public TypeCItemOrdered(string id, string itemType, string description, decimal price)
            : base(id, itemType, description, price)
        {

        }
    }
    public class OrderItem
    {
        public string Id { get; }
        public string ItemType { get; }
        public string Description { get; }
        public decimal Price { get; }

        public OrderItem(string id, string itemType, string description, decimal price)
        {
            Id = id;
            ItemType = itemType;
            Description = description;
            Price = price;
        }
    }
}

