using System;
using System.Threading.Tasks;
using NamedPipesFullDuplex.Utilities;

namespace NamedPipesFullDuplex.Interfaces
{
    public interface ICommunication
    {

          /// <summary>
        /// This event is fired when a message is received 
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;


        /// <summary>
        /// Starts the communication channel
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the communication channel
        /// </summary>
        void Stop();

        /// <summary>
        /// This method sends the given message asynchronously over the communication channel
        /// </summary>
        /// <param name="message"></param>
        /// <returns>A task of TaskResult</returns>
        void SendMessage(PipeMessage message);
    }



    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public PipeMessage Message { get; set; }
    }


}
