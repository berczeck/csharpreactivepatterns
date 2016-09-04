using System;
using Akka.Actor;

namespace Channel_Adapter
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var tradingBus = system.ActorOf<TradingBus>("tradingBus");
                var stockTrader =
                    system.ActorOf(Props.Create(() => new StockTrader(tradingBus,new BuyerService(), new SellerService())), "stockTrader");

                stockTrader.Tell(new ExecuteBuyOrder());
                stockTrader.Tell(new ExecuteSellOrder());
            }
            Console.ReadLine();
        }
    }

    public class StockTrader : ReceiveActor
    {
        public StockTrader(IActorRef tradingBus, BuyerService buyerService, SellerService sellerService)
        {
            Receive<ExecuteBuyOrder>(x =>
            {
                var result = buyerService.PlaceBuyOrder();
                tradingBus.Tell(new TradingNotification ("BuyOrderExecuted", result));
            });
            Receive<ExecuteSellOrder>(x =>
            {
                var result = buyerService.PlaceBuyOrder();
                tradingBus.Tell(new TradingNotification ("SellOrderExecuted", result));
            });

            tradingBus.Tell(new RegisterCommandHandler());
            tradingBus.Tell(new RegisterCommandHandler());
        }
    }

    public class TradingBus : ReceiveActor
    {
        public TradingBus()
        {
            Receive<TradingNotification>(x => Console.WriteLine($"TradingBus: received {x.Type}"));
            ReceiveAny(x => Console.WriteLine($"TradingBus: received {x}"));
        }
    }

    public class ServiceResult
    {
        public int Value { get;  }

        public ServiceResult(int value)
        {
            Value = value;
        }
    }

    public class BuyerService
    {
        public ServiceResult PlaceBuyOrder() => new ServiceResult (100);
    }
    public class SellerService
    {
        public ServiceResult PlaceSellOrder() => new ServiceResult (200);
    }
    public class RegisterCommandHandler { }
    public class Command { }
    public class ExecuteBuyOrder : Command { }
    public class ExecuteSellOrder : Command { }
    public class Event { }
    public class BuyOrderExecuted : Event { }
    public class SellOrderExecuted : Event { }

    public class TradingNotification
    {
        public string Type { get; }
        public ServiceResult Result { get; }

        public TradingNotification(string type, ServiceResult result)
        {
            Type = type;
            Result = result;
        }
    }
}
