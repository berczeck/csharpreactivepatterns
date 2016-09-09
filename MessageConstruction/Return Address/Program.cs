using System;
using Akka.Actor;

namespace Return_Address
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var client = system.ActorOf<Client>("client");
                var server = system.ActorOf<Server>("server");

                client.Tell(new StartWith(server));
            }
            Console.ReadLine();
        }
    }

    public class Client : ReceiveActor
    {
        public Client()
        {
            Receive<StartWith>(x =>
            {
                Console.WriteLine("Client: is starting...");
                x.Reference.Tell(new Request("REQ - 1"));
                x.Reference.Tell(new RequestComplex("REQ - 20"));
            });
            Receive<Reply>(x => Console.WriteLine($"Client: received response: {x.Message}"));
            Receive<ReplyToComplex>(x => Console.WriteLine($"Client: received reply to complex: {x.Message}"));
        }
    }

    public class Worker : ReceiveActor
    {
        public Worker()
        {
            Receive<WorkerRequestComplex>(x =>
            {
                Console.WriteLine($"Worker: received complex request value: {x.Message} from {Sender.Path}");
                Sender.Tell(new ReplyToComplex($"RESP-2000 for {x.Message}"));
            });
        }
    }

    public class Server : ReceiveActor
    {
        private IActorRef worker;
        public Server()
        {
            Receive<Request>(x =>
            {
                Console.WriteLine($"Server: received request value: {x.Message}");
                Sender.Tell(new Reply($"RESP - 1 for {x.Message}"));
            });
            Receive<RequestComplex>(x =>
            {
                Console.WriteLine($"Server: received request value: {x.Message}");
                Sender.Tell(new Reply($"RESP - 1 for {x.Message}"));
                worker.Forward(new WorkerRequestComplex(x.Message));
            });
        }

        protected override void PreStart()
        {
            worker = Context.ActorOf<Worker>("worker");
            base.PreStart();
        }
    }

    public class Request : ServerMessage
    {
        public Request(string message) : base(message) { }
    }
    public class RequestComplex : ServerMessage
    {
        public RequestComplex(string message) : base(message) { }
    }
    public class ServerMessage 
    {
        public string Message { get; }
        public ServerMessage(string message)
        {
            Message = message;
        }
    }
    public class WorkerRequestComplex
    {
        public string Message { get; }
        public WorkerRequestComplex(string message)
        { 
            Message = message;
        }
    }
    public class ClientMessage { }
    public class Reply : ClientMessage
    {
        public string Message { get; }
        public Reply(string message)
        {
            Message = message;
        }
    }
    public class ReplyToComplex : ClientMessage
    {
        public string Message { get; }
        public ReplyToComplex(string message)
        {
            Message = message;
        }
    }
    public class StartWith : ClientMessage
    {
        public IActorRef Reference { get; }
        public StartWith(IActorRef reference)
        {
            Reference = reference;
        }
    }
}
