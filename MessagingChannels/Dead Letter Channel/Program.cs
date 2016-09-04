using System;
using Akka.Actor;
using Akka.Event;

namespace Dead_Letter_Channel
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var sysListener = system.ActorOf<SysListener>("sysListener");
                system.EventStream.Subscribe(sysListener, typeof(DeadLetter));

                var deadActor = system.ActorOf<DeadActor>("deadActor");
                deadActor.Tell(PoisonPill.Instance);

                deadActor.Tell("Message to dead actor");
            }
            Console.ReadLine();
        }
    }
    public class DeadActor : ReceiveActor
    {
    }
    public class SysListener : ReceiveActor
    {
        public SysListener()
        {
            Receive<DeadLetter>(x => Console.WriteLine($"SysListner, DeadLetter received: {x}"));
        }   
    }
}
