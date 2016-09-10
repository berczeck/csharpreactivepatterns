using System;
using Akka.Actor;

namespace HelloWorldAkka
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new actor system (a container for your actors)
            using (var system = ActorSystem.Create("system"))
            {
                // Create your actor and get a reference to it.
                // This will be an "ActorRef", which is not a
                // reference to the actual actor instance
                // but rather a client or proxy to it.
                var helloWorldActor = system.ActorOf<HellowWorldActor>("helloWorldActor");

                // Send a message to the actor
                helloWorldActor.Tell(new HelloWorld("berczeck"));

                // This prevents the app from exiting
                // before the async work is done
                Console.ReadLine();
            }
        }
    }

    public class HellowWorldActor : ReceiveActor
    {
        public HellowWorldActor()
        {
            // Tell the actor to respond to the HelloWorld message
            Receive<HelloWorld>(hello => Console.WriteLine(hello.Mesage));
        }
    }

    public class HelloWorld
    {
        public string Mesage { get; }
        public HelloWorld(string name)
        {
            Mesage = $"HelloWorld Akka.NET {name}";
        }
    }
}
