using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

                var t1 = marketAnalysisTools.Ask(new Start());
                var t2 = portfolioManager.Ask(new Start());
                var t3 = stockTrader.Ask(new Start());

                Task.WaitAll(t1, t2, t3);
                
                tradingBus.Tell(new Status());
                
                tradingBus.Tell(new TradingCommand { CommandId = "ExecuteBuyOrder", Command = new ExecuteBuyOrder()});
                tradingBus.Tell(new TradingCommand { CommandId = "ExecuteSellOrder" , Command = new ExecuteSellOrder()});
                tradingBus.Tell(new TradingCommand { CommandId = "ExecuteBuyOrder", Command = new ExecuteBuyOrder()});
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

            Receive<RegisterNotificationInterest>(x =>
            {
                _notificationInterests.Add(x);
            });

            Receive<TradingCommand>(x =>
            {
                var commands =_commandHandlers.Where(c => c.CommandId == x.CommandId).ToList();
                commands.ForEach(c => c.Handler.Tell(x.Command));
            });

            Receive<TradingNotification>(x =>
            {
                var notifications = _notificationInterests.Where(c => c.NotificationId == x.NotificationId).ToList();
                notifications.ForEach(c => c.Interested.Tell(x.Notification));
            });

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
            Receive<ExecuteBuyOrder>(x =>
            {
                Console.WriteLine($"StockTrader: buying for: {x}");
                tradingBus.Tell(new TradingNotification {NotificationId = "BuyOrderExecuted", Notification = new BuyOrderExecuted()});
            });
            Receive<ExecuteSellOrder>(x =>
            {
                Console.WriteLine($"StockTrader: selling for: {x}");
                tradingBus.Tell(new TradingNotification {NotificationId = "SellOrderExecuted", Notification = new SellOrderExecuted()});
            });
            Receive<Start>(x =>
            {
                tradingBus.Tell(new RegisterCommandHandler {CommandId = "ExecuteBuyOrder", Handler = Self});
                tradingBus.Tell(new RegisterCommandHandler {CommandId = "ExecuteSellOrder", Handler = Self});
                Sender.Tell("ok");
            });
        }
    }

    public class PortfolioManager : ReceiveActor
    {
        public PortfolioManager(IActorRef tradingBus)
        {

            Receive<BuyOrderExecuted>(x => Console.WriteLine($"PortfolioManager: adding holding: {x}"));
            Receive<SellOrderExecuted>(x => Console.WriteLine($"PortfolioManager: adjusting holding: {x}"));
            Receive<Start>(x =>
            {
                tradingBus.Tell(new RegisterNotificationInterest
                {
                    NotificationId = "BuyOrderExecuted",
                    Interested = Self
                });
                tradingBus.Tell(new RegisterNotificationInterest
                {
                    NotificationId = "SellOrderExecuted",
                    Interested = Self
                });
                Sender.Tell("ok");
            });
        }
    }

    public class MarketAnalysisTools : ReceiveActor
    {
        public MarketAnalysisTools(IActorRef tradingBus)
        {
            Receive<BuyOrderExecuted>(x => Console.WriteLine($"MarketAnalysisTools: adding holding: {x}"));
            Receive<SellOrderExecuted>(x => Console.WriteLine($"MarketAnalysisTools: adjusting holding: {x}"));
            Receive<Start>(x =>
            {
                tradingBus.Tell(new RegisterNotificationInterest
                {
                    NotificationId = "BuyOrderExecuted",
                    Interested = Self
                });
                tradingBus.Tell(new RegisterNotificationInterest
                {
                    NotificationId = "SellOrderExecuted",
                    Interested = Self
                });
                Sender.Tell("ok");
            });
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

    public class TradingCommand : TradingBusMessage
    {
        public string CommandId { get; set; }
        public Command Command { get; set; }
    }

    public class TradingNotification : TradingBusMessage
    {
        public string NotificationId { get; set; }
        public Notification Notification { get; set; }
    }

    public class Status : TradingBusMessage { }
    public class Start { }
}
