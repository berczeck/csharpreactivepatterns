using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace Scatter_Gather
{
    class Program
    {
        static void Main(string[] args)
        {
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
                var duration = TimeSpan.FromSeconds(2);
                Context.System.Scheduler.ScheduleTellOnce(duration, Self, new PriceQuoteTimedOut(x.Id),ActorRefs.NoSender);
            });
            Receive<PriceQuoteFulfilled>(x =>
            {
                Console.WriteLine($"PriceQuoteAggregator: fulfilled price quote: {x}");

            });
        }

        private BestPriceQuotation BestPriceQuotationFrom(QuotationFulfillment quotationFulfillment)
        {
            var bestPrices = quotationFulfillment
                .PriceQuotes.GroupBy(x => x.ItemId)
                .Select(x => new { x.Key, BestPrice = x.OrderByDescending(t => t.DiscountPrice).First() })
                .Select(x => x.BestPrice).ToList();
            return new BestPriceQuotation(quotationFulfillment.Id, bestPrices);
        }

        private void QuoteBestPrice(QuotationFulfillment quotationFulfillment)
        {
            FulfilledPriceQuotes.Where(x => x.Id == quotationFulfillment.Id).ToList().ForEach(x =>
            {
                quotationFulfillment.Requester.Tell(BestPriceQuotationFrom(quotationFulfillment));
                FulfilledPriceQuotes.Remove(x);
            });
            //if (FulfilledPriceQuotes.Any(x => x.Id == quotationFulfillment.Id))
            //{
            //    quotationFulfillment.Requester.Tell(BestPriceQuotationFrom(quotationFulfillment));
            //    FulfilledPriceQuotes.Remove(FulfilledPriceQuotes.Where(x => x.Id == quotationFulfillment.Id).First());
            //}
        }

        private void PriceQuoteRequestTimedOut(string rfqId)
        {
            FulfilledPriceQuotes.Where(x => x.Id == rfqId).ToList().ForEach(x =>
            {
                QuoteBestPrice(x);
            });
        }

        private void PriceQuoteRequestFulfilled(PriceQuote priceQuoteFulfilled)
        {
            var previousFulfillment = FulfilledPriceQuotes.Where(x => x.Id == priceQuoteFulfilled.Id).First();
            var currentPriceQuotes = previousFulfillment.PriceQuotes.ToList();
            currentPriceQuotes.Add(priceQuoteFulfilled);
            var currentFulfillment = previousFulfillment.Create(currentPriceQuotes);
            if (currentPriceQuotes.Count >= currentFulfillment.QuotesRequested)
            {
                QuoteBestPrice(currentFulfillment);
            }
            else
            {
                FulfilledPriceQuotes.Remove(previousFulfillment);
                FulfilledPriceQuotes.Add(currentFulfillment);
            }
        }
    }

    public class BudgetHikersPriceQuotes : ReceiveActor
    {
        public BudgetHikersPriceQuotes(IActorRef interestRegistrar)
        {
            Receive<Start>(x =>
            {
                interestRegistrar.Tell(new SubscribeToPriceQuoteRequests(Self.Path.Name,Self));
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
                interestRegistrar.Tell(new SubscribeToPriceQuoteRequests(Self.Path.Name, Self));
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
                interestRegistrar.Tell(new SubscribeToPriceQuoteRequests(Self.Path.Name, Self));
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
                interestRegistrar.Tell(new SubscribeToPriceQuoteRequests(Self.Path.Name, Self));
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
                interestRegistrar.Tell(new SubscribeToPriceQuoteRequests(Self.Path.Name, Self));
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
    public class RetailItem
    {
        public string Id { get; }
        public decimal RetailPrice { get; }
        public RetailItem(string id, decimal retailPrice)
        {
            Id = id;
            RetailPrice = retailPrice;
        }
    }
    public class RequestForQuotation
    {
        public string Id { get; set; }
        public decimal TotalRetailPrice() => RetailItems.Sum(x => x.RetailPrice);
        public List<RetailItem> RetailItems { get; }
        public RequestForQuotation(string id, List<RetailItem> retailItems)
        {
            Id = id;
            RetailItems = retailItems;
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
    public class PriceQuote
    {
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
            => $"{nameof(PriceQuote)} {nameof(Id)}:{Id} {nameof(ItemId)}:{ItemId} {nameof(RetailPrice)}:{RetailPrice} {nameof(DiscountPrice)}:{DiscountPrice}";
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
        public override string ToString() => $"{nameof(PriceQuoteFulfilled)} {nameof(PriceQuote)}:{PriceQuote.ToString()}";
    }
    public class PriceQuoteTimedOut : AggregatorMessage
    {
        public string Id { get; }
        public PriceQuoteTimedOut(string id)
        {
            Id = id;
        }
        public override string ToString() => $"{nameof(PriceQuoteTimedOut)} {nameof(Id)}:{Id}";
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

        public override string ToString() => $"{nameof(QuotationFulfillment)} {nameof(Id)}:{Id} {nameof(QuotesRequested)}:{QuotesRequested} {nameof(Requester)}:{Requester}";
    }

    public class BestPriceQuotation
    {
        public string Id { get; }
        public IReadOnlyList<PriceQuote> PriceQuotes { get; }
        public BestPriceQuotation(string id, List<PriceQuote> priceQuotes)
        {
            Id = id;
            PriceQuotes = priceQuotes;
        }
        
        public override string ToString() => $"{nameof(BestPriceQuotation)} {nameof(Id)}:{Id}";
    }
    public class SubscribeToPriceQuoteRequests
    {
        public string QuoterId { get; }
        public IActorRef QuoteProcessor { get; }
        public SubscribeToPriceQuoteRequests(string quoterId, IActorRef quoteProcessor)
        {
            QuoterId = quoterId;
            QuoteProcessor = quoteProcessor;
        }
    }
    public class Start { }
}
