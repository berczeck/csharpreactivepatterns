using System;
using Akka.Actor;

namespace Point_to_Point_Channel
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var actorB = system.ActorOf<ActorB>("actorB");
                var actorA = system.ActorOf(Props.Create(() => new ActorA(actorB)), "actorA");
                var actorC = system.ActorOf(Props.Create(() => new ActorC(actorB)), "actorC");
            }
            Console.ReadLine();
        }
    }

    public class ActorA : ReceiveActor
    {
        public ActorA(IActorRef actorB)
        {
            actorB.Tell("Hello, from actor A!");
            actorB.Tell("Hello again, from actor A!");
        }
    }

    public class ActorB : ReceiveActor
    {
        public ActorB()
        {
            Receive<string>(x => Console.WriteLine($"ActorB: received {x}"));
        }
    }

    public class ActorC : ReceiveActor
    {
        public ActorC(IActorRef actorC)
        {
            actorC.Tell("Hello, from actor C!");
            actorC.Tell("Hello again, from actor C!");
        }
    }
}
