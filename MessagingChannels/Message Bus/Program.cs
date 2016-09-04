using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;

namespace Message_Bus
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var tradingBus = system.ActorOf<TradingBus>("tradingBus");
                var marketAnalysisTools = system.ActorOf(Props.Create(() => new MarketAnalysisTools(tradingBus)),
                    "marketAnalysisTools");
                var portfolioManager = system.ActorOf(Props.Create(() => new PortfolioManager(tradingBus)),
                    "portfolioManager");
                var stockTrader = system.ActorOf(Props.Create(() => new StockTrader(tradingBus)), "stockTrader");

                tradingBus.Tell(new Status());

                tradingBus.Tell(new TradingCommand { CommandId = "ExecuteBuyOrder" });
                tradingBus.Tell(new TradingCommand { CommandId = "ExecuteSellOrder" });
                tradingBus.Tell(new TradingCommand { CommandId = "ExecuteBuyOrder" });
            }
            Console.ReadLine();
        }
    }

    public class TradingBus : ReceiveActor
    {
        private IList<RegisterCommandHandler> _commandHandlers = new List<RegisterCommandHandler>();
        private IList<RegisterNotificationInterest> _notificationInterests = new List<RegisterNotificationInterest>();

        public TradingBus()
        {
            Receive<RegisterCommandHandler>(x =>
            {
                _commandHandlers.Add(x);
            });

            Receive<RegisterNotificationInterest>(x => _notificationInterests.Add(x));

            Receive<TradingCommand>(x => _commandHandlers.Where(c => c.CommandId == x.CommandId).ForEach(c => c.Handler.Tell(x)));

            Receive<TradingNotification>(x => _notificationInterests.Where(c => c.NotificationId == x.NotificationId).ForEach(c => c.Interested.Tell(x)));

            Receive<Status>(x =>
            {
                Console.WriteLine("TradingBus: STATUS:");
                _commandHandlers.ForEach(c => Console.WriteLine($"{c.CommandId} {c.Handler}"));
                Console.WriteLine("TradingBus: STATUS:");
                _notificationInterests.ForEach(c => Console.WriteLine($"{c.NotificationId} {c.Interested}"));
            });
        }
    }

    public class StockTrader : ReceiveActor
    {
        public StockTrader(IActorRef tradingBus)
        {
            tradingBus.Tell(new RegisterCommandHandler { CommandId = "ExecuteBuyOrder", Handler = Self });
            tradingBus.Tell(new RegisterCommandHandler { CommandId = "ExecuteSellOrder", Handler = Self });
            
            Receive<ExecuteBuyOrder>(x =>
            {
                Console.WriteLine($"StockTrader: buying for: {x}");
                tradingBus.Tell(new TradingNotification { NotificationId = "BuyOrderExecuted" });
            });
            Receive<ExecuteSellOrder>(x =>
            {
                Console.WriteLine($"StockTrader: selling for: {x}");
                tradingBus.Tell(new TradingNotification { NotificationId = "SellOrderExecuted" });
            });
        }
    }

    public class PortfolioManager : ReceiveActor
    {
        public PortfolioManager(IActorRef tradingBus)
        {
            tradingBus.Tell(new RegisterNotificationInterest { NotificationId = "BuyOrderExecuted", Interested = Self });
            tradingBus.Tell(new RegisterNotificationInterest { NotificationId = "SellOrderExecuted", Interested = Self });

            Receive<BuyOrderExecuted>(x => Console.WriteLine($"PortfolioManager: adding holding: {x}"));
            Receive<SellOrderExecuted>(x => Console.WriteLine($"PortfolioManager: adjusting holding: {x}"));
        }
    }

    public class MarketAnalysisTools : ReceiveActor
    {
        public MarketAnalysisTools(IActorRef tradingBus)
        {
            tradingBus.Tell(new RegisterNotificationInterest { NotificationId = "BuyOrderExecuted", Interested = Self });
            tradingBus.Tell(new RegisterNotificationInterest { NotificationId = "SellOrderExecuted", Interested = Self });

            Receive<BuyOrderExecuted>(x => Console.WriteLine($"MarketAnalysisTools: adding holding: {x}"));
            Receive<SellOrderExecuted>(x => Console.WriteLine($"MarketAnalysisTools: adjusting holding: {x}"));
        }
    }

    public class Command { }
    public class ExecuteBuyOrder : Command { }
    public class ExecuteSellOrder : Command { }

    public class Notification { }
    public class BuyOrderExecuted : Notification { }
    public class SellOrderExecuted : Notification { }

    public class TradingBusMessage { }

    public class RegisterCommandHandler : TradingBusMessage
    {
        public string CommandId { get; set; }
        public IActorRef Handler { get; set; }
    }

    public class RegisterNotificationInterest : TradingBusMessage
    {
        public string NotificationId { get; set; }
        public IActorRef Interested { get; set; }
    }
    public class TradingCommand : TradingBusMessage { public string CommandId { get; set; } }
    public class TradingNotification : TradingBusMessage { public string NotificationId { get; set; } }
    public class Status : TradingBusMessage { }
}
