using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Format_Indicator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var stockTrader = system.ActorOf<StockTrader>("stockTrader");

                stockTrader.Tell(new ExecuteBuyOrder(1,DateTime.Now.AddDays(-2)));
                stockTrader.Tell(new ExecuteBuyOrder(2, DateTime.Now.AddDays(-2)));
            }
            Console.ReadLine();
        }
    }

    public class StockTrader : ReceiveActor
    {
        public StockTrader()
        {
            Receive<ExecuteBuyOrder>(x =>
            {
                var orderExecutionStartedOn = x.Version == 1 ? DateTime.UtcNow : x.DateTimeOrdered;
                Console.WriteLine($"StockTrader: orderExecutionStartedOn: {orderExecutionStartedOn}");
            });
        }
    }

    public class ExecuteBuyOrder
    {
        public int Version { get; }
        public DateTime DateTimeOrdered { get; set; }
        public ExecuteBuyOrder(int version, DateTime dateTimeOrdered)
        {
            Version = version;
            DateTimeOrdered = dateTimeOrdered;
        }
    }
    

}
