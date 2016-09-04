using System;
using System.Text;
using Akka.Actor;

namespace Invalid_Message_Channel
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var invalidMessageChannel = system.ActorOf<InvalidMessageChannel>("invalidMessageChannel");
                var nextFilter = system.ActorOf<NextFilter>("nextFilter");
                var authenticator =
                    system.ActorOf(Props.Create(() => new Authenticator(nextFilter, invalidMessageChannel)), "authenticator");

                authenticator.Tell("Invalid message");
            }
            Console.ReadLine();
        }
    }

    public class ProcessIncomingOrder
    {
        public byte[] Message { get; set; }
    }

    public class InvalidMessage
    {
        public object Message { get; set; }
    }

    public class InvalidMessageChannel : ReceiveActor
    {
        public InvalidMessageChannel()
        {
            Receive<InvalidMessage>(x => Console.WriteLine("InvalidMessageChannel: InvalidMessage received, message") );
        }
    }
    
    public class Authenticator : ReceiveActor
    {
        public Authenticator(IActorRef nextFilter, IActorRef invalidMessageChannel)
        {
            Receive<ProcessIncomingOrder>(x =>
            {
                var text = Encoding.Default.GetString(x.Message);
                Console.WriteLine($"Decrypter: processing {text}");
                var orderText = text.Replace("(encryption)", string.Empty);
                nextFilter.Tell(new ProcessIncomingOrder {Message = Encoding.Default.GetBytes(orderText)});
            });
            ReceiveAny(x => invalidMessageChannel.Tell(new InvalidMessage {Message = x}));
        }
    }

    public class NextFilter : ReceiveActor
    {
    }
}
