using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;

namespace Pipes_and_Filters
{
    class Program
    {
        static void Main(string[] args)
        {
            var orderText = "(encryption)(certificate)<order id='123'>...</order>";
            var rawOrderBytes = Encoding.Default.GetBytes(orderText);
            using (var system = ActorSystem.Create("system"))
            {
                var filter5 = system.ActorOf<OrderManagerSystem>("orderManagerSystem");
                var filter4 = system.ActorOf(Props.Create(() => new Deduplicator(filter5)), "deduplicator");
                var filter3 = system.ActorOf(Props.Create(() => new Authenticator(filter4)), "authenticator");
                var filter2 = system.ActorOf(Props.Create(() => new Decrypter(filter3)), "decrypter");
                var filter1 = system.ActorOf(Props.Create(() => new OrderAcceptanceEndpoint(filter2)), "orderAcceptanceEndpoint");

                filter1.Tell(rawOrderBytes);
                filter1.Tell(rawOrderBytes);
            }
            Console.ReadLine();
        }
    }

    public class ProcessIncomingOrder {
        public byte[] Message { get; set; }
    }

    public class OrderAcceptanceEndpoint : ReceiveActor
    {
        public OrderAcceptanceEndpoint(IActorRef nextFilter)
        {
            Receive<byte[]>(x =>
            {
                Console.WriteLine($"OrderAcceptanceEndpoint: processing {Encoding.Default.GetString(x)}");
                nextFilter.Tell(new ProcessIncomingOrder {Message = x});
            });
        }
    }

    public class Decrypter : ReceiveActor
    {
        public Decrypter(IActorRef nextFilter)
        {
            Receive<ProcessIncomingOrder>(x =>
            {
                var text = Encoding.Default.GetString(x.Message);
                Console.WriteLine($"Decrypter: processing  {text}");
                var orderText = text.Replace("(encryption)", string.Empty);
                nextFilter.Tell(new ProcessIncomingOrder { Message = Encoding.Default.GetBytes(orderText)});
            });
        }
    }

    public class Authenticator : ReceiveActor
    {
        public Authenticator(IActorRef nextFilter)
        {
            Receive<ProcessIncomingOrder>(x =>
            {
                var text = Encoding.Default.GetString(x.Message);
                Console.WriteLine($"Authenticator: processing  {text}");
                var orderText = text.Replace("(certificate)", string.Empty);
                nextFilter.Tell(new ProcessIncomingOrder { Message = Encoding.Default.GetBytes(orderText) });
            });
        }
    }

    public class Deduplicator : ReceiveActor
    {
        private readonly List<string> _processedOrderIds = new List<string>();

        public Deduplicator(IActorRef nextFilter)
        {
            Receive<ProcessIncomingOrder>(x =>
            {
                var text = Encoding.Default.GetString(x.Message);
                Console.WriteLine($"Deduplicator: processing  {text}");
                var orderId = OrderIdFrom(text);

                if (!_processedOrderIds.Contains(orderId))
                {
                    _processedOrderIds.Add(orderId);
                    nextFilter.Tell(x);
                }
                else
                {
                    Console.WriteLine($"Deduplicator: found duplicate order {orderId}");
                }
            });
        }

        private string OrderIdFrom(string orderText)
        {
            var orderIdIndex = orderText.IndexOf("id='") + 4;
            var orderIdLastIndex = orderText.IndexOf("'", orderIdIndex);
            return orderText.Substring(orderIdIndex, orderIdLastIndex);
        }
    }

    public class OrderManagerSystem : ReceiveActor
    {
        public OrderManagerSystem()
        {
            Receive<ProcessIncomingOrder>(x =>
            {
                var text = Encoding.Default.GetString(x.Message);
                Console.WriteLine($"OrderManagementSystem: processing unique order: {text}");
            });
        }
    }
}
