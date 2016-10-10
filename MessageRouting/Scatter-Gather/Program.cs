using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Scatter_Gather
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
                    new List<RetailItem> { new RetailItem(29.95m, "1"), new RetailItem(99.95m, "2"),
                        new RetailItem(14.95m, "3") }));

                orderProcessor.Tell(
                   new RequestForQuotation("2",
                   new List<RetailItem> { new RetailItem(39.99m, "4"), new RetailItem(199.95m, "5"),
                       new RetailItem(149.95m, "6"), new RetailItem(724.99m, "7") }));

                orderProcessor.Tell(
                   new RequestForQuotation("3",
                   new List<RetailItem> { new RetailItem(119.99m, "8"), new RetailItem(499.95m, "9"),
                       new RetailItem(519.00m, "10"), new RetailItem(209.50m, "11") }));

                orderProcessor.Tell(
                   new RequestForQuotation("4",
                   new List<RetailItem> { new RetailItem(0.97m, "12"), new RetailItem(9.50m, "13"),
                        new RetailItem(1.99m, "14") }));

                orderProcessor.Tell(
                  new RequestForQuotation("5",
                  new List<RetailItem> { new RetailItem(107.50m, "15"), new RetailItem(9.50m, "16"),
                       new RetailItem(599.99m, "17"), new RetailItem(249.95m, "18"),
                       new RetailItem(789.99m, "19") }));

                Console.ReadLine();
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
                    Context.System.Scheduler.ScheduleTellOnce(duration, Self, new PriceQuoteTimedOut(x.Id), ActorRefs.NoSender);
                    FulfilledPriceQuotes.Add(new QuotationFulfillment(x.Id, x.QuotesRequested, new List<PriceQuote>(), Sender));
                });
                Receive<PriceQuoteFulfilled>(x =>
                {
                    Console.WriteLine($"PriceQuoteAggregator: fulfilled price quote: {x}");
                    PriceQuoteRequestFulfilled(x.PriceQuote);
                });
                Receive<PriceQuoteTimedOut>(x => PriceQuoteRequestTimedOut(x.Id));
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

        public class MountaineeringSuppliesOrderProcessor : ReceiveActor
        {
            private readonly List<SubscribeToPriceQuoteRequests> Subscribers = new List<SubscribeToPriceQuoteRequests>();

            public MountaineeringSuppliesOrderProcessor(IActorRef priceQuoteAggregator)
            {
                Receive<SubscribeToPriceQuoteRequests>(x =>
                {
                    Subscribers.Add(x);
                });
                Receive<PriceQuote>(x => priceQuoteAggregator.Tell(new PriceQuoteFulfilled(x)));
                Receive<RequestForQuotation>(x =>
                {
                    priceQuoteAggregator.Tell(new RequiredPriceQuotesForFulfillment(x.Id, Subscribers.Count * x.RetailItems.Count));
                    Dispatch(x);
                });
                Receive<BestPriceQuotation>(x => Console.WriteLine($"OrderProcessor: received: {x}"));
                ReceiveAny(x => Console.WriteLine($"OrderProcessor: unexpected: {x}"));
            }

            private void Dispatch(RequestForQuotation request)
            {
                Subscribers.ForEach(x => request.RetailItems.ForEach(retailItem =>
                {
                    Console.WriteLine($"OrderProcessor: {request.Id} item: {retailItem.Id} to: {x.QuoterId}");
                    x.QuoteProcessor.Tell(new RequestPriceQuote(request.Id, retailItem.Id, retailItem.RetailPrice, request.TotalRetailPrice()));
                }));
            }
        }

        public class BudgetHikersPriceQuotes : ReceiveActor
        {
            public BudgetHikersPriceQuotes(IActorRef interestRegistrar)
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
            public RetailItem(decimal retailPrice, string id)
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
}
