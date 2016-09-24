using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Aggregator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var priceQuoteAggregator =
                    system.ActorOf<PriceQuoteAggregator>("priceQuoteAggregator");
                var orderProcessor =
                    system.ActorOf(Props.Create(() =>
                    new MountaineeringSuppliesOrderProcessor(priceQuoteAggregator)), "mountaineeringSuppliesOrderProcessor");

                var budgetHikersPriceQuotes =
                    system.ActorOf(Props.Create(() =>
                    new BudgetHikersPriceQuotes(orderProcessor)), "budgetHikersPriceQuotes");
                var highSierraPriceQuotes =
                    system.ActorOf(Props.Create(() =>
                    new HighSierraPriceQuotes(orderProcessor)), "highSierraPriceQuotes");
                var mountainAscent =
                   system.ActorOf(Props.Create(() =>
                   new MountainAscentPriceQuotes(orderProcessor)), "mountainAscent");
                var pinnacleGear =
                   system.ActorOf(Props.Create(() =>
                   new PinnacleGearPriceQuotes(orderProcessor)), "pinnacleGear");
                var rockBottomOuterwear =
                   system.ActorOf(Props.Create(() =>
                   new RockBottomOuterwearPriceQuotes(orderProcessor)), "rockBottomOuterwear");

                var t1 = budgetHikersPriceQuotes.Ask(new Start());
                var t2 = highSierraPriceQuotes.Ask(new Start());
                var t3 = mountainAscent.Ask(new Start());
                var t4 = pinnacleGear.Ask(new Start());
                var t5 = rockBottomOuterwear.Ask(new Start());

                Task.WaitAll(t1, t2, t3, t4, t5);

                orderProcessor.Tell(
                    new RequestForQuotation("1",
                    new List<RetailItem> { new RetailItem("1",29.95m), new RetailItem("2",99.95m),
                        new RetailItem("3",14.95m) }));

                orderProcessor.Tell(
                   new RequestForQuotation("2",
                   new List<RetailItem> { new RetailItem("4",39.99m), new RetailItem("5",199.95m),
                       new RetailItem("6",149.95m), new RetailItem("7",724.99m) }));

                orderProcessor.Tell(
                   new RequestForQuotation("3",
                   new List<RetailItem> { new RetailItem("8",119.99m), new RetailItem("9",499.95m),
                       new RetailItem("10",519.00m), new RetailItem("11",209.50m) }));

                orderProcessor.Tell(
                   new RequestForQuotation("4",
                   new List<RetailItem> { new RetailItem("12",0.97m), new RetailItem("13",9.50m),
                        new RetailItem("14",1.99m) }));

                orderProcessor.Tell(
                  new RequestForQuotation("5",
                  new List<RetailItem> { new RetailItem("15",107.50m), new RetailItem("16",9.50m),
                       new RetailItem("17",599.99m), new RetailItem("18",249.95m),
                       new RetailItem( "19",789.99m) }));
                
                Console.ReadLine();
            }
        }
    }

    public class PriceQuoteAggregator : ReceiveActor
    {
        private readonly List<QuotationFulfillment> FulfilledPriceQuotes = new List<QuotationFulfillment>();
        public PriceQuoteAggregator()
        {
            Receive<RequiredPriceQuotesForFulfillment>(x =>
            {
                Console.WriteLine($"PriceQuoteAggregator: required fulfilled: {x}");
                FulfilledPriceQuotes.Add(new QuotationFulfillment(x.Id, x.QuotesRequested, new List<PriceQuote>(), Sender));
            });
            Receive<PriceQuoteFulfilled>(x =>
            {
                Console.WriteLine($"PriceQuoteAggregator: fulfilled price quote: {x}");
                var previousFulfillment = FulfilledPriceQuotes.First(p => p.Id == x.PriceQuote.Id);

                var currentPriceQuotes = previousFulfillment.PriceQuotes.ToList();
                currentPriceQuotes.Add(x.PriceQuote);
                var currentFulfillment = previousFulfillment.Create(currentPriceQuotes);

                if (currentPriceQuotes.Count >= currentFulfillment.QuotesRequested)
                {
                    currentFulfillment.Requester.Tell(currentFulfillment);
                    FulfilledPriceQuotes.Remove(previousFulfillment);
                }
                else
                {
                    FulfilledPriceQuotes.Add(currentFulfillment);
                }
            });
        }
    }
    public class MountaineeringSuppliesOrderProcessor : ReceiveActor
    {
        private readonly List<PriceQuoteInterest> InterestRegistry = new List<PriceQuoteInterest>();

        public MountaineeringSuppliesOrderProcessor(IActorRef priceQuoteAggregator)
        {
            Receive<PriceQuoteInterest>(x => InterestRegistry.Add(x));
            Receive<PriceQuote>(x =>
            {
                priceQuoteAggregator.Tell(new PriceQuoteFulfilled(x));
                Console.WriteLine($"OrderProcessor: received: {x}");
            });
            Receive<RequestForQuotation>(x =>
            {
                var recipientList = CalculateRecipientList(x);
                priceQuoteAggregator.Tell(new RequiredPriceQuotesForFulfillment(x.Id,recipientList.Count * x.RetailItems.Count));
                DispatchTo(recipientList, x);
            });
            Receive<QuotationFulfillment>(x => Console.WriteLine($"OrderProcessor: received: {x}"));
            ReceiveAny(x => Console.WriteLine($"OrderProcessor: unexpected: {x}"));
        }

        private List<IActorRef> CalculateRecipientList(RequestForQuotation rfq) 
            => InterestRegistry
            .Where(x => rfq.TotalRetailPrice >= x.LowTotalRetail && rfq.TotalRetailPrice <= x.HighTotalRetail)
            .Select(x => x.QuoteProcessor).ToList();

        private void DispatchTo(List<IActorRef> recipientList, RequestForQuotation rfq)
        {
            recipientList.ForEach(recipient =>
            {
                rfq.RetailItems.ToList().ForEach(retailItem =>
                {
                    Console.WriteLine($"OrderProcessor: {rfq.Id} item: {retailItem.Id} to: {recipient.Path}");
                    recipient.Tell(new RequestPriceQuote(rfq.Id, retailItem.Id, retailItem.RetailPrice, rfq.TotalRetailPrice));
                });
            });
        }
    }
    public class BudgetHikersPriceQuotes : ReceiveActor
    {
        public BudgetHikersPriceQuotes(IActorRef interestRegistrar)
        {
            Receive<Start>(x =>
            {
                interestRegistrar.Tell(new PriceQuoteInterest(Self.Path.ToString(), Self, 1, 1000));
                Sender.Tell("ok");
            });
            Receive<RequestPriceQuote>(x =>
            {
                var retailPrice = x.RetailPrice;
                var orderTotalRetailPrice = x.OrderTotalRetailPrice;
                var discount = DiscountPercentage(retailPrice * orderTotalRetailPrice);
                Sender.Tell(new PriceQuote(x.Id,x.ItemId,x.RetailPrice, retailPrice- discount));
            });
        }

        private decimal DiscountPercentage(decimal orderTotalRetailPrice)
        {
            if (orderTotalRetailPrice <= 100.00m) return 0.02M;
            if (orderTotalRetailPrice <= 399.99m) return 0.03M;
            if (orderTotalRetailPrice <= 499.99m) return 0.05M;
            if (orderTotalRetailPrice <= 799.99m) return 0.07M;
            return 0.075M;
        }
    }
    public class HighSierraPriceQuotes : ReceiveActor
    {
        public HighSierraPriceQuotes(IActorRef interestRegistrar)
        {
            Receive<Start>(x =>
            {
                interestRegistrar.Tell(new PriceQuoteInterest(Self.Path.ToString(), Self, 100, 10000));
                Sender.Tell("ok");
            });
            Receive<RequestPriceQuote>(x =>
            {
                var retailPrice = x.RetailPrice;
                var orderTotalRetailPrice = x.OrderTotalRetailPrice;
                var discount = DiscountPercentage(retailPrice * orderTotalRetailPrice);
                Sender.Tell(new PriceQuote(x.Id, x.ItemId, x.RetailPrice, retailPrice - discount));
            });
        }
        private decimal DiscountPercentage(decimal orderTotalRetailPrice)
        {
            if (orderTotalRetailPrice <= 150.00m) return 0.015m;
            if (orderTotalRetailPrice <= 499.99m) return 0.02m;
            if (orderTotalRetailPrice <= 999.99m) return 0.03m;
            if (orderTotalRetailPrice <= 4999.99m) return 0.04m;
            return 0.05m;
        }
    }
    public class MountainAscentPriceQuotes : ReceiveActor
    {
        public MountainAscentPriceQuotes(IActorRef interestRegistrar)
        {
            Receive<Start>(x =>
            {
                interestRegistrar.Tell(new PriceQuoteInterest(Self.Path.ToString(), Self, 70, 5000));
                Sender.Tell("ok");
            });
            Receive<RequestPriceQuote>(x =>
            {
                var retailPrice = x.RetailPrice;
                var orderTotalRetailPrice = x.OrderTotalRetailPrice;
                var discount = DiscountPercentage(retailPrice * orderTotalRetailPrice);
                Sender.Tell(new PriceQuote(x.Id, x.ItemId, x.RetailPrice, retailPrice - discount));
            });
        }
        private decimal DiscountPercentage(decimal orderTotalRetailPrice)
        {
            if (orderTotalRetailPrice <= 99.99m) return 0.01m;
            if (orderTotalRetailPrice <= 199.99m) return 0.02m;
            if (orderTotalRetailPrice <= 499.99m) return 0.03m;
            if (orderTotalRetailPrice <= 799.99m) return 0.04m;
            if (orderTotalRetailPrice <= 999.99m) return 0.05m;
            if (orderTotalRetailPrice <= 2999.99m) return 0.0475m;
            return 0.05m;
        }
    }
    public class PinnacleGearPriceQuotes : ReceiveActor
    {
        public PinnacleGearPriceQuotes(IActorRef interestRegistrar)
        {
            Receive<Start>(x =>
            {
                interestRegistrar.Tell(new PriceQuoteInterest(Self.Path.ToString(), Self, 250, 500000));
                Sender.Tell("ok");
            });
            Receive<RequestPriceQuote>(x =>
            {
                var retailPrice = x.RetailPrice;
                var orderTotalRetailPrice = x.OrderTotalRetailPrice;
                var discount = DiscountPercentage(retailPrice * orderTotalRetailPrice);
                Sender.Tell(new PriceQuote(x.Id, x.ItemId, x.RetailPrice, retailPrice - discount));
            });
        }
        private decimal DiscountPercentage(decimal orderTotalRetailPrice)
        {
            if (orderTotalRetailPrice <= 299.99m) return 0.015m;
            if (orderTotalRetailPrice <= 399.99m) return 0.0175m;
            if (orderTotalRetailPrice <= 499.99m) return 0.02m;
            if (orderTotalRetailPrice <= 999.99m) return 0.03m;
            if (orderTotalRetailPrice <= 1199.99m) return 0.035m;
            if (orderTotalRetailPrice <= 4999.99m) return 0.04m;
            if (orderTotalRetailPrice <= 7999.99m) return 0.05m;
            return 0.06m;
        }
    }
    public class RockBottomOuterwearPriceQuotes : ReceiveActor
    {
        public RockBottomOuterwearPriceQuotes(IActorRef interestRegistrar)
        {
            Receive<Start>(x =>
            {
                interestRegistrar.Tell(new PriceQuoteInterest(Self.Path.ToString(), Self, 0.5m, 7500));
                Sender.Tell("ok");
            });
            Receive<RequestPriceQuote>(x =>
            {
                var retailPrice = x.RetailPrice;
                var orderTotalRetailPrice = x.OrderTotalRetailPrice;
                var discount = DiscountPercentage(retailPrice * orderTotalRetailPrice);
                Sender.Tell(new PriceQuote(x.Id, x.ItemId, x.RetailPrice, retailPrice - discount));
            });
        }
        private decimal DiscountPercentage(decimal orderTotalRetailPrice)
        {
            if (orderTotalRetailPrice <= 100.00m) return 0.015m;
            if (orderTotalRetailPrice <= 399.99m) return 0.02m;
            if (orderTotalRetailPrice <= 499.99m) return 0.03m;
            if (orderTotalRetailPrice <= 799.99m) return 0.04m;
            if (orderTotalRetailPrice <= 999.99m) return 0.05m;
            if (orderTotalRetailPrice <= 2999.99m) return 0.06m;
            if (orderTotalRetailPrice <= 4999.99m) return 0.07m;
            if (orderTotalRetailPrice <= 5999.99m) return 0.075m;
            return 0.08m;
        }
    }
    public class PriceQuoteInterest
    {
        public string Path { get; }
        public IActorRef QuoteProcessor { get; }
        public decimal LowTotalRetail { get; }
        public decimal HighTotalRetail { get; }
        public PriceQuoteInterest(string path, IActorRef quoteProcessor, decimal lowTotalRetail, decimal highTotalRetail)
        {
            Path = path;
            QuoteProcessor = quoteProcessor;
            LowTotalRetail = lowTotalRetail;
            HighTotalRetail = highTotalRetail;
        }
    }
    public class RequestPriceQuote
    {
        public string Id { get; }
        public string ItemId { get; }
        public decimal RetailPrice { get; }
        public decimal OrderTotalRetailPrice { get; }
        public RequestPriceQuote(string id, string itemId, decimal retailPrice, decimal orderTotalRetailPrice)
        {
            Id = id;
            ItemId = itemId;
            RetailPrice = retailPrice;
            OrderTotalRetailPrice = orderTotalRetailPrice;
        }
    }
    public class RequestForQuotation
    {
        public string Id { get;  }
        public IReadOnlyList<RetailItem> RetailItems { get; }
        public RequestForQuotation(string id, List<RetailItem> retailItems)
        {
            Id = id;
            RetailItems = retailItems;
        }
        public decimal TotalRetailPrice => RetailItems.Sum(x => x.RetailPrice);
    }
    public class RetailItem
    {
        public string Id { get; }
        public decimal RetailPrice { get; }
        public RetailItem(string id,decimal retailPrice)
        {
            Id = id;
            RetailPrice = retailPrice;
        }
    }
    public class QuotationFulfillment
    {
        public string Id { get; }
        public int QuotesRequested { get; }
        public IReadOnlyList<PriceQuote> PriceQuotes { get; }
        public IActorRef Requester { get; }
        public QuotationFulfillment(string id, int quotesRequested, List<PriceQuote> priceQuotes, IActorRef requester)
        {
            Id = id;
            QuotesRequested = quotesRequested;
            PriceQuotes = priceQuotes;
            Requester = requester;
        }

        public QuotationFulfillment Create(List<PriceQuote> priceQuotes) 
            => new QuotationFulfillment(Id, QuotesRequested, priceQuotes, Requester);

        public override string ToString() => $"{Id} {QuotesRequested} {Requester}";
    }
    public class PriceQuote {
        public string Id { get; }
        public string ItemId { get; }
        public decimal RetailPrice { get; }
        public decimal DiscountPrice { get; }
        public PriceQuote(string id, string itemId, decimal retailPrice, decimal discountPrice)
        {
            Id = id;
            ItemId = itemId;
            RetailPrice = retailPrice;
            DiscountPrice = discountPrice;
        }

        public override string ToString()
        {
            return $"{Id} {ItemId} {RetailPrice} {DiscountPrice}";
        }
    }
    public class AggregatorMessage { }
    public class RequiredPriceQuotesForFulfillment : AggregatorMessage
    {
        public string Id { get; }
        public int QuotesRequested { get; }
        public RequiredPriceQuotesForFulfillment(string id, int quotesRequested)
        {
            Id = id;
            QuotesRequested = quotesRequested;
        }
    }
    public class PriceQuoteFulfilled : AggregatorMessage
    {
        public PriceQuote PriceQuote { get; }
        public PriceQuoteFulfilled(PriceQuote priceQuote)
        {
            PriceQuote = priceQuote;
        }
        public override string ToString() => PriceQuote.ToString();
    }
    public class Start { }
}
