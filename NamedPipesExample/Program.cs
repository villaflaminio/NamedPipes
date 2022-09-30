using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using NamedPipesFullDuplex.Client;
using NamedPipesFullDuplex.Interfaces;
using NamedPipesFullDuplex.Server;

namespace NamedPipesExample
{
    internal class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("log4net.config"));
                XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));
            }
            catch (Exception e)
            {
                _logger.Error(e);
                _logger.Debug("No default logging cofiguration loaded");
            }


            _logger.Debug("start program");
            IPipeServer _server = new PipeServer("flaminio", 10);
            IPipeServer _server2 = new PipeServer("server", 10);
            IPipeClient _client = new PipeClient(_server.ServerId);
            IPipeClient _client2 = new PipeClient("server");

            _server.Start();
            _server2.Start();
            Console.WriteLine("Server c# avviato");

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


            _server2.MessageReceivedEvent += (sender, argss) =>
            {
                message = argss.Message;
                Console.WriteLine("Server 2 ha ricevuto " + message);
            };


            _client.MessageReceivedEvent += (sender, argss) =>
            {
                message = argss.Message;
                Console.WriteLine("_client ha ricevuto " + message);
            };
            Task.Delay(1000);
            for (int i = 0; i < 10; i++)
            {
                _client.SendMessage("Client send essage " + i);
                //  _server.sendMessage("Server message " + i);
            }

            _client2.SendMessage("Client 2 message");


            _server.SendMessage("Server send message");


            _server.ClientDisconnectedEvent += (sender, argss) =>
            {
                message = argss.ClientId;
                Console.WriteLine("il client " + message + " si e' disconnesso");
            };
            _server.ClientConnectedEvent += (sender, argss) =>
            {
                message = argss.ClientId;
                Console.WriteLine("il client " + message + " si e' connesso");
            };

            Console.ReadLine();
        }
    }
}
