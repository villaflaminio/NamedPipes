using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using NamedPipesFullDuplex.Client;
using NamedPipesFullDuplex.Interfaces;
using NamedPipesFullDuplex.Server;
using NamedPipesFullDuplex.Utilities;

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


            _logger.Debug("Server c# avviato");

            IPipeServer _server = new PipeServer("flaminio", 10);
            IPipeServer _server2 = new PipeServer("server", 10);
            IPipeClient _client = new PipeClient(_server.ServerId);
            IPipeClient _client2 = new PipeClient("server");

            _server.Start();
            _server2.Start();
            //  Console.WriteLine("Server c# avviato");

            PipeMessage message = null;


            _server.Start();
            _client.Start();
            _client2.Start();

            PipeMessage pipe = new PipeMessage("flaminio", "client message");

            _server.MessageReceivedEvent += (sender, argss) =>
            {
                message = argss.Message;
               // Console.WriteLine("Server ha ricevuto " + message);
            };


            _server2.MessageReceivedEvent += (sender, argss) =>
            {
                message = argss.Message;
              //  Console.WriteLine("Server 2 ha ricevuto " + message);
            };


            _client.MessageReceivedEvent += (sender, argss) =>
            {
                message = argss.Message;
              //  Console.WriteLine("_client ha ricevuto " + message);
            };
            Task.Delay(1000);
            for (int i = 0; i < 10; i++)
            {
                _client.SendMessage(pipe);
                //  _server.sendMessage("Server message " + i);
            }

            _client2.SendMessage(pipe);


            _server.SendMessage(pipe);


            _server.ClientDisconnectedEvent += (sender, argss) =>
            {
              _logger.Info("Client disconnected " + argss.ClientId);
                //  Console.WriteLine("il client " + message + " si e' disconnesso");
            };
            _server.ClientConnectedEvent += (sender, argss) =>
            {
                _logger.Info("Client connected " + argss.ClientId);
                //  Console.WriteLine("il client " + message + " si e' connesso");
            };

            Console.ReadLine();
        }
    }
}
