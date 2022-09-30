using NamedPipesFullDuplex.Utilities;
using System;
using System.Threading.Tasks;

namespace NamedPipesFullDuplex.Interfaces
{
    public interface IPipeServer : ICommunication
    {
        /// <summary>
        /// The server id
        /// </summary>
        string ServerId { get; }


        /// <summary>
        /// This event is fired when a client connects 
        /// </summary>
        event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;

        /// <summary>
        /// This event is fired when a client disconnects 
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

    }

}
