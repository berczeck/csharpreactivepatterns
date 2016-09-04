using System;
using Akka.Actor;

namespace Publish_Subscribe_Channel
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var quoteListener = system.ActorOf<QuoteListener>("quoteListener");
                system.EventStream.Subscribe(quoteListener, typeof(PricedQuote));
                system.EventStream.Publish(new PricedQuote());
            }
            Console.ReadLine();
        }
    }

    public class Symbol { }
    public class Money { }
    public class Market { }
    public class PricedQuote { }

    public class QuoteListener : ReceiveActor
    {
        public QuoteListener()
        {
            Receive<PricedQuote>(x => Console.WriteLine("QuoteListener: PricedQuoted received"));
        }
    }
}
