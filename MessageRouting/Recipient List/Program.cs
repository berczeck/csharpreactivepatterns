using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Recipient_List
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var orderProcessor = 
                    system.ActorOf<MountaineeringSuppliesOrderProcessor>("orderProcessor");
                
                var budgetHikersPriceQuotes = 
                    system.ActorOf(Props.Create(()=> 
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
    }
    public class InterestRegistry
    {
        public string Path { get; }
        public PriceQuoteInterest PriceQuoteInterest { get; }
        public InterestRegistry(string path, PriceQuoteInterest priceQuoteInterest)
        {
            Path = path;
            PriceQuoteInterest = priceQuoteInterest;
        }
    }
    public class MountaineeringSuppliesOrderProcessor : ReceiveActor
    {
        private readonly IList<InterestRegistry> interestRegistryList = new List<InterestRegistry>();
        public MountaineeringSuppliesOrderProcessor()
        {
            Receive<PriceQuoteInterest>(x => interestRegistryList.Add(new InterestRegistry(x.Path, x)));
            Receive<PriceQuote>(x => Console.WriteLine($"OrderProcessor: received: {x} from {Sender.Path}"));
            Receive<RequestForQuotation>(x =>
            {
                var recipientList = CalculateRecipientList(x);
                DispatchTo(x, recipientList);
            });
            ReceiveAny(x => Console.WriteLine($"OrderProcessor: unexpected: {x}"));
        }

        private void DispatchTo(RequestForQuotation rfq, List<IActorRef> recipientList)
        {
            recipientList.ForEach(x => rfq.RetailItems.ForEach(t =>
            {
                Console.WriteLine($"OrderProcessor: {rfq.Id} item: {t.ItemId} to: {x.Path}");
                x.Tell(new RequestPriceQuote(t.RetailPrice, rfq.TotalRetailPrice()));
            }));
        }

        private List<IActorRef> CalculateRecipientList(RequestForQuotation rfq)
            => interestRegistryList.Where(x =>
            rfq.TotalRetailPrice() >= x.PriceQuoteInterest.LowTotalRetail &&
            rfq.TotalRetailPrice() <= x.PriceQuoteInterest.HighTotalRetail)
            .Select(x => x.PriceQuoteInterest.QuoteProcessor).ToList();
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
                Sender.Tell(new PriceQuote());
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
                Sender.Tell(new PriceQuote());
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
                Sender.Tell(new PriceQuote());
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
                Sender.Tell(new PriceQuote());
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
                Sender.Tell(new PriceQuote());
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
    public class Start { }
    public class RetailItem
    {
        public decimal RetailPrice { get; }
        public string ItemId { get; }
        public RetailItem(decimal retailPrice, string itemId)
        {
            RetailPrice = retailPrice;
            ItemId = itemId;
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
        public decimal RetailPrice { get; }
        public decimal OrderTotalRetailPrice { get; }
        public RequestPriceQuote(decimal retailPrice, decimal orderTotalRetailPrice)
        {
            RetailPrice = retailPrice;
            OrderTotalRetailPrice = orderTotalRetailPrice;
        }
    }
    public class PriceQuote { }
}
