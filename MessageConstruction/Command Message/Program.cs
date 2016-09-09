using System;
using Akka.Actor;

namespace Command_Message
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var actor = system.ActorOf<StockTrader>("stockTrader");
                actor.Tell(new ExecuteBuyOrder());
                actor.Tell(new ExecuteSellOrder());
            }
            Console.ReadLine();
        }
    }

    public class StockTrader : ReceiveActor
    {
        public StockTrader()
        {
            Receive<ExecuteBuyOrder>(x => Console.WriteLine($"StockTrader: buying for: {x}"));
            Receive<ExecuteSellOrder>(x => Console.WriteLine($"StockTrader: selling for: {x}"));
        }
    }

    public class TradingCommand { }
    public class ExecuteBuyOrder : TradingCommand { }
    public class ExecuteSellOrder : TradingCommand { }
}
