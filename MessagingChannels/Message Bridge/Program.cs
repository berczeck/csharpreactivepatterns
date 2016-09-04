using Akka.Actor;
using static System.Console;

namespace Message_Bridge
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var inventoryProductAllocationBridge =
                    system.ActorOf<InventoryProductAllocationBridge>("inventoryProductAllocationBridge");
                inventoryProductAllocationBridge.Tell(new RabbitMQTextMessage("Rabbit test message"));
            }
            ReadLine();
        }
    }

    public class InventoryProductAllocationBridge : ReceiveActor
    {
        public InventoryProductAllocationBridge()
        {
            Receive<RabbitMQTextMessage>(x =>
            {
                WriteLine($"InventoryProductAllocationBridge: received {x.Message}");
                var inventoryProductAllocation = TranslatedToInventoryProductAlloction(x.Message);
                WriteLine($"InventoryProductAllocationBridge: translated {inventoryProductAllocation}");
                AcknowledgeDelivery(x);
            });
        }

        private void AcknowledgeDelivery(RabbitMQTextMessage textMessage) => WriteLine($"InventoryProductAllocationBridge: acknowledged {textMessage.Message}");
        private string TranslatedToInventoryProductAlloction(string message) => $"Inventory product alloction for {message}";
    }

    public class RabbitMQTextMessage
    {
        public string Message { get; }

        public RabbitMQTextMessage(string message)
        {
            Message = message;
        }
    }
}
