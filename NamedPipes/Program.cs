using ClientServerUsingNamedPipes.Client;
using ClientServerUsingNamedPipes.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            PipeServer _server = new PipeServer("flaminio" , 10);
            PipeClient _client = new PipeClient(_server.ServerId);
            PipeClient _client2 = new PipeClient("server");

            _server.Start();

            string message = null;


            _server.Start();
            _client.Start();
           _client2.Start();

            // Act
            _client.SendMessage("Client's message");

            _server.MessageReceivedEvent += (sender, argss) =>
            {
                message = argss.Message;
                Console.WriteLine("Server ha ricevuto " + message);
            };


            Task.Delay(1000);
            for (int i = 0; i < 10; i++)
            {
                _client.SendMessage("Client send essage " + i);
                //  _server.sendMessage("Server message " + i);
            }

          //  _client2.SendMessage("Client 2 message");


            _server.SendMessage("Server send message");
            _server.ClientDisconnectedEvent += (sender, argss) =>
            {
                message = argss.ClientId;
                Console.WriteLine("il client " + message + " si e' disconnesso");
            };

                      Console.ReadLine();
        }
    }
}
