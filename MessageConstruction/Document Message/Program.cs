using System;
using Akka.Actor;

namespace Document_Message
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var quotation = system.ActorOf<Quotation>("quotation");
                var requester = system.ActorOf(Props.Create(()=> new Requester(quotation)), "requester");
            }
            Console.ReadLine();
        }
    }
    
    public class Quotation : ReceiveActor
    {
        public Quotation()
        {
            Receive<RequestPriceQuote>(x => Sender.Tell(new QuotationFulfillment(new PriceQuote(), Sender)));
        }
    }

    public class Requester : ReceiveActor
    {
        public Requester(IActorRef quotation)
        {
            ReceiveAny(x => Console.WriteLine($"Requester: quote {x}"));
            quotation.Tell(new RequestPriceQuote());
        }
    }

    public class PriceQuote { }
    public class QuotationFulfillment
    {
        public PriceQuote Price { get; }
        public IActorRef Requester { get; }

        public QuotationFulfillment(PriceQuote price, IActorRef requester)
        {
            Price = price;
            Requester = requester;
        }
    }
    public class RequestPriceQuote { }
}
