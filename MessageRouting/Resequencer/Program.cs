using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace Resequencer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var sequencedMessageConsumer = system.ActorOf<SequencedMessageConsumer>("sequencedMessageConsumer");
                var resequencerConsumer =
                    system.ActorOf(Props.Create(() => new ResequencerConsumer(sequencedMessageConsumer)), "resequencerConsumer");
                var chaosRouter =
                    system.ActorOf(Props.Create(() => new ChaosRouter(resequencerConsumer)), "chaosRouter");

                int total = 10;
                Enumerable.Range(1, total).ToList()
                    .ForEach(x => chaosRouter.Tell(new SequencedMessage("ABC", x, total)));
                Enumerable.Range(1, total).ToList()
                    .ForEach(x => chaosRouter.Tell(new SequencedMessage("XYZ", x, total)));

                Console.ReadLine();
            }
        }
    }
    
    public class ChaosRouter : ReceiveActor
    {
        private Random random = new Random();
        public ChaosRouter(IActorRef consumer)
        {
            Receive<SequencedMessage>(sequencedMessage =>
            {
                var millis = random.Next(100);
                Console.WriteLine($"ChaosRouter: delaying delivery of {sequencedMessage} for {millis} milliseconds");
                var duration = TimeSpan.FromMilliseconds(millis);
                Context.System.Scheduler.ScheduleTellOnce(duration, consumer, sequencedMessage, ActorRefs.NoSender);
            });
        }

    }
    public class ResequencerConsumer : ReceiveActor
    {

        private readonly IDictionary<string, ResequencedMessages> Resequenced =
            new Dictionary<string, ResequencedMessages>();

        public ResequencerConsumer(IActorRef actualConsumer)
        {
            Receive<SequencedMessage>(unsequencedMessage =>
            {
                Console.WriteLine($"ResequencerConsumer: received: {unsequencedMessage}");
                Resequence(unsequencedMessage);
                DispatchAllSequenced(unsequencedMessage.CorrelationId, actualConsumer);
                RemoveCompleted(unsequencedMessage.CorrelationId);
            });
        }

        private List<SequencedMessage> DummySequencedMessages(int count)
            => Enumerable.Range(1, count).Select(x => new SequencedMessage("", -1, x)).ToList();

        private void Resequence(SequencedMessage sequencedMessage)
        {
            if (!Resequenced.ContainsKey(sequencedMessage.CorrelationId))
            {
                Resequenced.Add(sequencedMessage.CorrelationId, new ResequencedMessages(1, DummySequencedMessages(sequencedMessage.Total)));
            }
            Resequenced[sequencedMessage.CorrelationId].SequencedMessages[sequencedMessage.Index - 1] = sequencedMessage;
        }

        private void DispatchAllSequenced(string correlationId,IActorRef actualConsumer)
        {
            var resequencedMessage = Resequenced[correlationId];

            var dispatchableIndex = resequencedMessage.DispatchableIndex;
            resequencedMessage.SequencedMessages
                .Where(x => x.Index == resequencedMessage.DispatchableIndex).ToList()
                .ForEach(x =>
                {
                    actualConsumer.Tell(x);
                    dispatchableIndex++;
                });
            
            Resequenced[correlationId] = resequencedMessage.AdvancedTo(dispatchableIndex);
            if(resequencedMessage.SequencedMessages.Any(x => x.Index == dispatchableIndex))
            {
                DispatchAllSequenced(correlationId, actualConsumer);
            }
        }

        private void RemoveCompleted(string correlationId)
        {
            var resequencedMessages = Resequenced[correlationId];
            var message = resequencedMessages.SequencedMessages[0];
            if (resequencedMessages.DispatchableIndex > message.Total)
            {
                Console.WriteLine($"ResequencerConsumer: removed completed: {correlationId}");
                Resequenced.Remove(correlationId);
            }
        }
    }

    public class SequencedMessageConsumer : ReceiveActor
    {       

        public SequencedMessageConsumer()
        {
            Receive<SequencedMessage>(x =>
            {
                Console.WriteLine($"SequencedMessageConsumer: received: {x}");
            });
        }
        
    }

    public class SequencedMessage
    {
        public string CorrelationId { get; }
        public int Index { get; }
        public int Total { get; }
        public SequencedMessage(string correlationId, int index, int total)
        {
            CorrelationId = correlationId;
            Index = index;
            Total = total;
        }

        public override string ToString() => $"{nameof(SequencedMessage)} {nameof(CorrelationId)}:{CorrelationId} {nameof(Index)}:{Index} {nameof(Total)}:{Total}";
    }

    public class ResequencedMessages
    {
        public int DispatchableIndex { get; }
        public List<SequencedMessage> SequencedMessages { get; }

        public ResequencedMessages(int dispatchableIndex, List<SequencedMessage> sequencedMessages)
        {
            DispatchableIndex = dispatchableIndex;
            SequencedMessages = sequencedMessages;
        }

        public ResequencedMessages AdvancedTo(int dispatchableIndex) 
            => new ResequencedMessages(dispatchableIndex, SequencedMessages.ToList());
    }
}
