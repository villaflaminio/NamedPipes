﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using NamedPipesFullDuplex.Client;
using System.Threading.Tasks;
using log4net;
using NamedPipesFullDuplex.Interfaces;
using NamedPipesFullDuplex.Utilities;

namespace NamedPipesFullDuplex.Server
{
    internal class InternalPipeServer 
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(InternalPipeServer));

        private readonly NamedPipeServerStream _pipeServer;
        private bool _isStopping;
        private readonly object _lockingObject = new object();

        public readonly string Id;

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;



        /// <summary>
        /// Creates a new NamedPipeServerStream 
        /// </summary>
        public InternalPipeServer(string pipeName, int maxNumberOfServerInstances)
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances,PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Id = Guid.NewGuid().ToString();
        }


        #region event
        /// <summary>
        /// This method fires MessageReceivedEvent with the given message
        /// </summary>
        private void OnMessageReceivedEvent(string message)
        {
            if (MessageReceivedEvent != null)
            {
                MessageReceivedEvent(this,
                    new MessageReceivedEventArgs
                    {
                        Message = message
                    });
            }
        }

        /// <summary>
        /// This method fires ConnectedEvent 
        /// </summary>
        private void OnConnected()
        {
            if (ClientConnectedEvent != null)
            {
                ClientConnectedEvent(this, new ClientConnectedEventArgs { ClientId = Id });
            }
        }

        /// <summary>
        /// This method fires DisconnectedEvent 
        /// </summary>
        private void OnDisconnected()
        {
            if (ClientDisconnectedEvent != null)
            {
                ClientDisconnectedEvent(this, new ClientDisconnectedEventArgs { ClientId = Id });
            }
        }

        #endregion

        #region public methods

        public string ServerId
        {
            get { return Id; }
        }

        /// <summary>
        /// This method begins an asynchronous operation to wait for a client to connect.
        /// </summary>
        public void Start()
        {
            try
            {
                _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// This callback is called when the async WaitForConnection operation is completed,
        /// whether a connection was made or not. WaitForConnection can be completed when the server disconnects.
        /// </summary>
        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            if (!_isStopping)
            {
                lock (_lockingObject)
                {
                    if (!_isStopping)
                    {
                        // Call EndWaitForConnection to complete the connection operation
                        _pipeServer.EndWaitForConnection(result);

                        OnConnected();

                        BeginRead(new BufferReading());
                    }
                }
            }
        }


        /// <summary>
        /// This method disconnects, closes and disposes the server
        /// </summary>
        public void Stop()
        {
            _isStopping = true;

            try
            {
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
            finally
            {
                _pipeServer.Close();
                _pipeServer.Dispose();
            }
        }



        /// <summary>
        /// This method begins an asynchronous read operation.
        /// </summary>
        private void BeginRead(BufferReading bufferReading)
        {
            try
            {
                _pipeServer.BeginRead(bufferReading.Buffer, 0, bufferReading.BufferSize, EndReadCallBack, bufferReading);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// This callback is called when the BeginRead operation is completed.
        /// We can arrive here whether the connection is valid or not
        /// </summary>
        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = _pipeServer.EndRead(result);
            if (readBytes > 0)
            {
                var info = (BufferReading)result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

               var message = info.StringBuilder.ToString().TrimEnd('\0');

                OnMessageReceivedEvent(message);

                // Begin a new reading operation
                BeginRead(new BufferReading());
                //}
            }
            /// When no bytes were read, it can mean that the client have been disconnected or some problem in BeginRead
            else
            {
                if (!_isStopping)
                {
                    lock (_lockingObject)
                    {
                        if (!_isStopping)
                        {
                            OnDisconnected();
                            Stop();
                        }
                    }
                }
            }
        }

    
         public Task<TaskResult> SendMessage(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskResult>();

            if (_pipeServer.IsConnected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                _pipeServer.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(EndWriteCallBack(asyncResult));
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }

                }, null);
            }
            else
            {
                _logger.Error("Cannot send message, pipe is not connected");
                throw new IOException("pipe is not connected");
            }

            return taskCompletionSource.Task;
        }


        public TaskResult EndWriteCallBack(IAsyncResult asyncResult)
        {
            _pipeServer.EndWrite(asyncResult);
            _pipeServer.Flush();

            return new TaskResult { IsSuccess = true };
        }

       
        public bool isConnected()
        {
            return _pipeServer.IsConnected;
        }

        #endregion
    }
}