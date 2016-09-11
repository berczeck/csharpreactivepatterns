using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Dynamic_Router
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var dunnoInterested = system.ActorOf<DunnoInterested>("dunnoInterested");
                var typedMessageInterestRouter = 
                    system.ActorOf(Props.Create(() => 
                    new TypedMessageInterestRouter(dunnoInterested)), "typedMessageInterestRouter");

                typedMessageInterestRouter.Ask(new Start()).Wait();

                var typeAInterested =
                    system.ActorOf(Props.Create(() =>
                    new TypeAInterested(typedMessageInterestRouter)), "typeAInterested");
                var typeBInterested =
                    system.ActorOf(Props.Create(() =>
                    new TypeBInterested(typedMessageInterestRouter)), "typeBInterested");
                var typeCInterested =
                    system.ActorOf(Props.Create(() =>
                    new TypeCInterested(typedMessageInterestRouter)), "typeCInterested");
                var typeCAlsoInterested =
                    system.ActorOf(Props.Create(() =>
                    new TypeCAlsoInterested(typedMessageInterestRouter)), "typeCAlsoInterested");

                var t1 = typeAInterested.Ask(new Start());
                var t2 = typeBInterested.Ask(new Start());
                var t3 = typeCInterested.Ask(new Start());
                var t4 = typeCAlsoInterested.Ask(new Start());

                Task.WaitAll(t1, t2, t3, t4);

                typedMessageInterestRouter.Tell(new TypeAMessage("Message of TypeA."));
                typedMessageInterestRouter.Tell(new TypeBMessage("Message of TypeB."));
                typedMessageInterestRouter.Tell(new TypeCMessage("Message of TypeC."));
                typedMessageInterestRouter.Tell(new TypeCMessage("Another message of TypeC."));
                typedMessageInterestRouter.Tell(new TypeDMessage("Message of TypeD."));
                Console.ReadLine();
            }
        }
    }

    public class TypedMessageInterestRouter : ReceiveActor
    {
        private readonly IDictionary<string, List<IActorRef>> interestRegistry = new Dictionary<string, List<IActorRef>>();
        public TypedMessageInterestRouter(IActorRef dunnoInterested)
        {
            Receive<InterestedIn>(x =>
            {
                Console.WriteLine($"Actor {Sender} registered for type {x.Message}");
                RegisterInterest(x.Message, Sender);
            });
            Receive<NoLongerInterestedIn>(x =>
            {
                UnRegisterInterest(x.Message, Sender);
            });
            Receive<TypeMessage>(x => SendFor(x, dunnoInterested));
            Receive<Start>(x => Sender.Tell("ok"));
        }

        private void SendFor(object message, IActorRef dunnoInterested)
        {
            var messageType = message.GetType().Name;
            if (!interestRegistry.ContainsKey(messageType))
            {
                dunnoInterested.Tell(message);
                return;
            }
            var interestedList = interestRegistry[messageType];
            interestedList.ForEach(x => x.Tell(message));
        }

        private void RegisterInterest(string messageType, IActorRef interested)
        {
            if (!interestRegistry.ContainsKey(messageType))
            {
                interestRegistry.Add(messageType, new List<IActorRef> { interested});
                return;
            }
            
            var interestedList = interestRegistry[messageType];
            var existsInterested = interestedList.Any(x => x == interested);
            if (!existsInterested)
            {
                interestedList.Add(interested);
                interestRegistry[messageType] = interestedList;
            }
        }

        private void UnRegisterInterest(string messageType, IActorRef wasInterested)
        {
            if (!interestRegistry.ContainsKey(messageType))
            {
                return;
            }
            var interestedList = interestRegistry[messageType];
            var existsInterested = interestedList.Any(x => x == wasInterested);
            if (existsInterested)
            {
                interestedList.Remove(wasInterested);
                interestRegistry[messageType] = interestedList;
                Console.WriteLine($"Actor {Sender} unregistered for type {messageType}");
            }
        }
    }

    public class TypeAInterested : ReceiveActor
    {
        public TypeAInterested(IActorRef interestRouter)
        {
            Receive<TypeAMessage>(x => Console.WriteLine($"TypeAInterested: received: {x.Message}"));
            Receive<Start>(x =>
            {
                interestRouter.Tell(new InterestedIn(typeof(TypeAMessage).Name));
                Sender.Tell("ok");
            });
        }
    }

    public class TypeBInterested : ReceiveActor
    {
        public TypeBInterested(IActorRef interestRouter)
        {
            Receive<TypeBMessage>(x => Console.WriteLine($"TypeBInterested: received: {x.Message}"));
            Receive<Start>(x =>
            {
                interestRouter.Tell(new InterestedIn(typeof(TypeBMessage).Name));
                Sender.Tell("ok");
            });
        }
    }

    public class TypeCInterested : ReceiveActor
    {
        public TypeCInterested(IActorRef interestRouter)
        {
            Receive<TypeCMessage>(x =>
            {
                Console.WriteLine($"TypeCInterested: received: {x.Message}");
                interestRouter.Tell(new NoLongerInterestedIn(typeof(TypeCMessage).Name));
            });
            Receive<Start>(x =>
            {
                interestRouter.Tell(new InterestedIn(typeof(TypeCMessage).Name));
                Sender.Tell("ok");
            });
        }
    }

    public class TypeCAlsoInterested : ReceiveActor
    {
        public TypeCAlsoInterested(IActorRef interestRouter)
        {
            Receive<TypeCMessage>(x =>
            {
                Console.WriteLine($"TypeCAlsoInterested: received: {x.Message}");
                interestRouter.Tell(new NoLongerInterestedIn(typeof(TypeCMessage).Name));
            });
            Receive<Start>(x =>
            {
                interestRouter.Tell(new InterestedIn(typeof(TypeCMessage).Name));
                Sender.Tell("ok");
            });
        }
    }

    public class DunnoInterested : ReceiveActor
    {
        public DunnoInterested()
        {
            ReceiveAny(x => Console.WriteLine($"DunnoInterest: received undeliverable message: {x}"));
        }
    }

    public class Interest
    {
        public string Message { get; }
        public Interest(string message)
        {
            Message = message;
        }
    }
    public class Start { }
    public class InterestedIn : Interest { public InterestedIn(string message):base(message){ } }
    public class NoLongerInterestedIn : Interest { public NoLongerInterestedIn(string message):base(message){ } }
    public class TypeMessage
    {
        public string Message { get; }
        public TypeMessage(string message)
        {
            Message = message;
        }
    }
    public class TypeAMessage : TypeMessage { public TypeAMessage(string message):base(message){ } }
    public class TypeBMessage : TypeMessage { public TypeBMessage(string message) : base(message) { } }
    public class TypeCMessage : TypeMessage { public TypeCMessage(string message) : base(message) { } }
    public class TypeDMessage : TypeMessage { public TypeDMessage(string message) : base(message) { } }
}
