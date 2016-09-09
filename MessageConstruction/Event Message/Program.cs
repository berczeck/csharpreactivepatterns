using System;
using Akka.Actor;

namespace Event_Message
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var subscriber = system.ActorOf<Subscriber>("subscriber");
                var quotation = system.ActorOf(Props.Create(() => new Quotation(subscriber)), "requester");

                quotation.Tell(new RequestPriceQuote());
            }
            Console.ReadLine();
        }
    }

    public class Quotation : ReceiveActor
    {
        public Quotation(IActorRef subscriber)
        {
            Receive<RequestPriceQuote>(x => subscriber.Tell(new PriceQuoteFulfilled(new PriceQuote())));
        }
    }

    public class Subscriber : ReceiveActor
    {
        public Subscriber()
        {
            ReceiveAny(x => Console.WriteLine($"Requester: event {x}"));
        }
    }

    public class PriceQuote { }
    public class RequestPriceQuote { }
    public class PriceQuoteFulfilled
    {
        public PriceQuote Price { get; }
        public PriceQuoteFulfilled(PriceQuote price)
        {
            Price = price;
        }
    }
}
