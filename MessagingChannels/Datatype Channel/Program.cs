using System;
using System.Text;
using Akka.Actor;

namespace Datatype_Channel
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var productQueriesChannel = system.ActorOf<ProductQueriesChannel>("productQueriesChannel");

                productQueriesChannel.Tell(new ProductQuery());
                productQueriesChannel.Tell(Encoding.UTF8.GetBytes("test query"));
            }
            Console.ReadLine();
        }
    }
    
    public class ProductQuery { }

    public class ProductQueriesChannel : ReceiveActor
    {
        public ProductQueriesChannel()
        {
            Receive<ProductQuery>(x =>
            {
                Console.WriteLine($"ProductQueriesChannel: ProductQuery received");
            });
        }
    }
}
