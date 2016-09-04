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
                
                tradingBus.Tell(new TradingCommand ("ExecuteBuyOrder", new ExecuteBuyOrder()));
                tradingBus.Tell(new TradingCommand ("ExecuteSellOrder" , new ExecuteSellOrder()));
                tradingBus.Tell(new TradingCommand ("ExecuteBuyOrder", new ExecuteBuyOrder()));
            }
            Console.ReadLine();
        }
    }

    public class TradingBus : ReceiveActor
    {
        private readonly IList<RegisterCommandHandler> _commandHandlers = new List<RegisterCommandHandler>();
        private readonly IList<RegisterNotificationInterest> _notificationInterests = new List<RegisterNotificationInterest>();

        public TradingBus()
        {
            Receive<RegisterCommandHandler>(x =>_commandHandlers.Add(x));

            Receive<RegisterNotificationInterest>(x => _notificationInterests.Add(x));

            Receive<TradingCommand>(x => 
            _commandHandlers.Where(c => c.CommandId == x.CommandId).ForEach(c => c.Handler.Tell(x.Command)));

            Receive<TradingNotification>(x => 
            _notificationInterests.Where(c => c.NotificationId == x.NotificationId).ForEach(c => c.Interested.Tell(x.Notification)));

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
                tradingBus.Tell(new TradingNotification ("BuyOrderExecuted", new BuyOrderExecuted()));
            });
            Receive<ExecuteSellOrder>(x =>
            {
                Console.WriteLine($"StockTrader: selling for: {x}");
                tradingBus.Tell(new TradingNotification ("SellOrderExecuted", new SellOrderExecuted()));
            });
            Receive<Start>(x =>
            {
                tradingBus.Tell(new RegisterCommandHandler ("ExecuteBuyOrder", Self));
                tradingBus.Tell(new RegisterCommandHandler ("ExecuteSellOrder", Self));
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
                tradingBus.Tell(new RegisterNotificationInterest("BuyOrderExecuted",Self));
                tradingBus.Tell(new RegisterNotificationInterest("SellOrderExecuted", Self));
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
                tradingBus.Tell(new RegisterNotificationInterest("BuyOrderExecuted",Self));
                tradingBus.Tell(new RegisterNotificationInterest("SellOrderExecuted", Self));
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
        public string CommandId { get; }
        public IActorRef Handler { get; }

        public RegisterCommandHandler(string commandId, IActorRef handler)
        {
            CommandId = commandId;
            Handler = handler;
        }
    }

    public class RegisterNotificationInterest : TradingBusMessage
    {
        public string NotificationId { get; }
        public IActorRef Interested { get; }

        public RegisterNotificationInterest(string notificationId, IActorRef interested)
        {
            NotificationId = notificationId;
            Interested = interested;
        }
    }

    public class TradingCommand : TradingBusMessage
    {
        public string CommandId { get; }
        public Command Command { get; }

        public TradingCommand(string commandId, Command command)
        {
            CommandId = commandId;
            Command = command;
        }
    }

    public class TradingNotification : TradingBusMessage
    {
        public string NotificationId { get; }
        public Notification Notification { get; }

        public TradingNotification(string notificationId, Notification notification)
        {
            NotificationId = notificationId;
            Notification = notification;
        }
    }

    public class Status : TradingBusMessage { }
    public class Start { }
}
