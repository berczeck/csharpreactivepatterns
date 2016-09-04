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
                tradingBus.Tell(new TradingNotification {Type = "BuyOrderExecuted", Result = result});
            });
            Receive<ExecuteSellOrder>(x =>
            {
                var result = buyerService.PlaceBuyOrder();
                tradingBus.Tell(new TradingNotification { Type = "SellOrderExecuted", Result = result});
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

    public class ServiceResult {
        public int Value { get; set; } }
    public class BuyerService
    {
        public ServiceResult PlaceBuyOrder() => new ServiceResult {Value = 100};
    }
    public class SellerService
    {
        public ServiceResult PlaceSellOrder() => new ServiceResult { Value = 200 };
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
        public string Type { get; set; }
        public ServiceResult Result { get; set; }
    }
}
