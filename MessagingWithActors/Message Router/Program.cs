using System;
using Akka.Actor;

namespace Message_Router
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var processor1 = system.ActorOf<Processor>("processor1");
                var processor2 = system.ActorOf<Processor>("processor2");
                var alternatingRouter = system.ActorOf(
                    Props.Create(() => new AlternatingRouter(processor1, processor2)), "alternatingRouter");

                for (int i = 0; i < 10; i++)
                {
                    alternatingRouter.Tell($"{i}");
                }
            }
            Console.ReadLine();
        }
    }

    public class AlternatingRouter : ReceiveActor
    {
        private int _alternate = 1;
        private readonly IActorRef _processor1;
        private readonly IActorRef _processor2;

        public AlternatingRouter(IActorRef processor1, IActorRef processor2)
        {
            _processor1 = processor1;
            _processor2 = processor2;

            Receive<string>(x =>
            {
                var processor = AlternateProcessor();
                Console.WriteLine($"AlternatingRouter: routing {x} to {processor.Path.Name}");
                processor.Tell(x);
            });
        }

        private IActorRef AlternateProcessor()
        {
            if (_alternate == 1)
            {
                _alternate = 2;
                return _processor1;
            }

            _alternate = 1;
            return _processor2;
        }
    }

    public class Processor : ReceiveActor
    {
        public Processor()
        {
            Receive<string>(x => Console.WriteLine($"Processor: {Self.Path.Name} received {x}"));
        }
    }
}