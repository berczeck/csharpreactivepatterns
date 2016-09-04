using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Message_Endpoint
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var discounter = system.ActorOf<Discounter>("discounter");
                var highSierraPriceQuotes = 
                    system.ActorOf(Props.Create(() => new HighSierraPriceQuotes(discounter)), "highSierraPriceQuotes");
                var requester = system.ActorOf(Props.Create(() => new Requester(highSierraPriceQuotes)), "requester");
                Console.WriteLine(requester.Path);
            }
            Console.ReadLine();
        }
    }

    public class QuoteMessage { }
    public class RequestPriceQuote : QuoteMessage { }

    public class DiscountPriceCalculated : QuoteMessage
    {
        public ActorSelection Requester { get; set; }
    }

    public class CalculatedDiscountPriceFor { }
    public class PriceQuote { }

    public class HighSierraPriceQuotes : ReceiveActor
    {
        public HighSierraPriceQuotes(IActorRef discounter)
        {
            Receive<RequestPriceQuote>(x =>
            {
                Console.WriteLine("HighSierraPriceQuotes: RequestPriceQuote received");
                discounter.Tell(new CalculatedDiscountPriceFor());
            });

            Receive<DiscountPriceCalculated>(x =>
            {
                Console.WriteLine("HighSierraPriceQuotes: DiscountPriceCalculated received");
                x.Requester.Tell(new PriceQuote());
            });
        }
    }

    public class Discounter : ReceiveActor
    {
        public Discounter()
        {
            Receive<CalculatedDiscountPriceFor>(x =>
            {
                Console.WriteLine("Discounter: CalculatedDiscountPriceFor received");
                Sender.Tell(new DiscountPriceCalculated {Requester = Context.ActorSelection("/user/requester") });
            });
        }
    }

    public class Requester : ReceiveActor
    {
        public Requester(IActorRef quotes)
        {
            Receive<PriceQuote>(x =>
            {
                Console.WriteLine("Requester: PriceQuote");
            });

            quotes.Tell(new RequestPriceQuote());
        }
    }
}
