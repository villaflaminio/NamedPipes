﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using NamedPipesFullDuplex.Client;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using NamedPipesFullDuplex.Interfaces;
using NamedPipesFullDuplex.Utilities;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Extensions;
using NamedPipesFullDuplex.logging;

namespace NamedPipesFullDuplex.Server
{
    public class PipeServer : IPipeServer
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PipeServer));

        private readonly string _pipeName;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IDictionary<string, InternalPipeServer> _servers; // ConcurrentDictionary is thread safe
        private int _maxNumberOfServerInstances = 10;
      
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;


        public PipeServer(string pipeName, int MaxNumberOfServerInstances)
        {
            try
            {
                _logger.Debug("Enter in constructor of PipeServer ");
                _logger.Trace(string.Format("Received parameters: pipeName: {0} , MaxNumberOfServerInstances : {1} ", pipeName, MaxNumberOfServerInstances));
                _pipeName = pipeName;
                _maxNumberOfServerInstances = MaxNumberOfServerInstances;
                _synchronizationContext = AsyncOperationManager.SynchronizationContext;
                _servers = new ConcurrentDictionary<string, InternalPipeServer>();
            }
            catch (Exception e)
            {
                _logger.Fatal(e);
            }
        }

        #region ICommunicationServer implementation

        public string ServerId
        {
            get { return _pipeName; }
        }

        public void Start()
        {
            _logger.Debug("Start PipeServer");
            StartNamedPipeServer();
        }

        public void Stop()
        {
            foreach (var server in _servers.Values)
            {
                try
                {
                    UnregisterFromServerEvents(server);
                    server.Stop();
                }
                catch (Exception)
                {
                    _logger.Error("Fialed to stop server");
                }
            }

            _servers.Clear();
        }

        #endregion

        #region event
        /// <summary>
        /// Fires MessageReceivedEvent in the current thread
        /// </summary>
        /// <param name="eventArgs"></param>
        private void OnMessageReceivedEvent(MessageReceivedEventArgs eventArgs)
        {
            _synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e),
                eventArgs);
        }

        /// <summary>
        /// Fires ClientConnectedEvent in the current thread
        /// </summary>
        /// <param name="eventArgs"></param>
        private void OnClientConnectedEvent(ClientConnectedEventArgs eventArgs)
        {
            _synchronizationContext.Post(e => ClientConnectedEvent.SafeInvoke(this, (ClientConnectedEventArgs)e),
                eventArgs);
        }

        /// <summary>
        /// Fires ClientDisconnectedEvent in the current thread
        /// </summary>
        /// <param name="eventArgs"></param>
        private void OnClientDisconnectedEvent(ClientDisconnectedEventArgs eventArgs)
        {
            _synchronizationContext.Post(
                e => ClientDisconnectedEvent.SafeInvoke(this, (ClientDisconnectedEventArgs)e), eventArgs);
        }
        
        /// <summary>
        /// Unregisters from the given server's events
        /// </summary>
        /// <param name="server"></param>
        private void UnregisterFromServerEvents(InternalPipeServer server)
        {
            server.ClientConnectedEvent -= ClientConnectedEventHandler;
            server.ClientDisconnectedEvent -= ClientDisconnectedEventHandler;
            server.MessageReceivedEvent -= MessageReceivedEventHandler;
        }

        #endregion

        #region event_handler
        /// <summary>
        /// Handles a client connection. Fires the relevant event and prepares for new connection.
        /// </summary>
        private void ClientConnectedEventHandler(object sender, ClientConnectedEventArgs eventArgs)
        {
            OnClientConnectedEvent(eventArgs);

            StartNamedPipeServer(); // Create a additional server as a preparation for new connection
        }

        /// <summary>
        /// Hanldes a client disconnection. Fires the relevant event ans removes its server from the pool
        /// </summary>
        private void ClientDisconnectedEventHandler(object sender, ClientDisconnectedEventArgs eventArgs)
        {
            OnClientDisconnectedEvent(eventArgs);

            StopNamedPipeServer(eventArgs.ClientId);
        }

        /// <summary>
        /// Handles a message that is received from the client. Fires the relevant event.
        /// </summary>
        private void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceivedEvent(eventArgs);
        }


        #endregion

        #region private methods

        /// <summary>
        /// Starts a new NamedPipeServerStream that waits for connection
        /// </summary>
        private void StartNamedPipeServer()
        {
            var server = new InternalPipeServer(_pipeName, _maxNumberOfServerInstances);
            _servers[server.Id] = server;

            server.ClientConnectedEvent += ClientConnectedEventHandler;
            server.ClientDisconnectedEvent += ClientDisconnectedEventHandler;
            server.MessageReceivedEvent += MessageReceivedEventHandler;

            server.Start();
        }

        /// <summary>
        /// Stops the server that belongs to the given id
        /// </summary>
        /// <param name="id"></param>
        private void StopNamedPipeServer(string id)
        {
            UnregisterFromServerEvents(_servers[id]);
            _servers[id].Stop();
            _servers.Remove(id);
        }
       

        /// <summary>
        /// Starts a new NamedPipeServerStream that waits for connection
        /// </summary>
        public Task<TaskResult> SendMessage(string message)
        {
            Task<TaskResult> result; 

            foreach (var server in _servers.Values)
            {

                if (server.isConnected())
                {
                    result = server.SendMessage(message);
                    Console.WriteLine(result.Result.ToString());
                }
            }
            return null;

        }

       
        #endregion
    }
}
