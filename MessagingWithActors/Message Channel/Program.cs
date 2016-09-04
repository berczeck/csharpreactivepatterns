using System;
using Akka.Actor;

namespace Message_Channel
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var actor = system.ActorOf<Processor>("processor");
                actor.Tell(new ProcessorMessage {X = 10, Y = 20, Z = 30});
            }
            Console.ReadLine();
        }
    }

    public class Processor : ReceiveActor
    {
        public Processor()
        {
            Receive<ProcessorMessage>(x => Console.WriteLine($"Processor: received ProcessJob {x.X} {x.Y} {x.Z}"));
        }
    }

    public class ProcessorMessage
    {
        public int  X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}
