using System;
using Akka.Actor;

namespace Request_Reply
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
            });
            Receive<Reply>(x => Console.WriteLine($"Client: received response: {x.Message}"));
        }
    }

    public class Server : ReceiveActor
    {
        public Server()
        {
            Receive<Request>(x =>
            {
                Console.WriteLine($"Server: received request value: {x.Message}");
                Sender.Tell(new Reply($"RESP - 1 for {x.Message}"));
            });
        }
    }

    public class ServerMessage { }
    public class Request : ServerMessage
    {
        public string Message { get; }
        public Request(string message)
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
    public class StartWith : ClientMessage
    {
        public IActorRef Reference { get; }
        public StartWith(IActorRef reference)
        {
            Reference = reference;
        }
    }
}
