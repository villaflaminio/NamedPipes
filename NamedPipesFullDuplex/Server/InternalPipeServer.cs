using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using NamedPipesFullDuplex.Client;
using System.Threading.Tasks;
using log4net;
using NamedPipesFullDuplex.Interfaces;
using NamedPipesFullDuplex.Utilities;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Extensions;
using NamedPipesFullDuplex.logging;

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
            try
            {
                _logger.Debug("Enter in constructor of InternalPipeServer ");
                _logger.Trace(string.Format("Received parameters: pipeName: {0} , maxNumberOfServerInstances : {1} ", pipeName, maxNumberOfServerInstances));
                _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                Id = Guid.NewGuid().ToString();
                _logger.Trace("New InternalPipeServer created with Id: " + Id);
            }
            catch (Exception e)
            {
                _logger.Fatal(e);
            }
        }


        #region event
        /// <summary>
        /// This method fires MessageReceivedEvent with the given message
        /// </summary>
        private void OnMessageReceivedEvent(string message)
        {
            try
            {
                _logger.Debug("Enter in OnMessageReceivedEvent " + message);

                if (MessageReceivedEvent != null)
                {
                    MessageReceivedEvent(this,
                        new MessageReceivedEventArgs
                        {
                            Message = message
                        });
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        /// <summary>
        /// This method fires ConnectedEvent 
        /// </summary>
        private void OnConnected()
        {
            if (ClientConnectedEvent != null)
            {
                _logger.Debug("Client " + Id + " Connected");
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
                _logger.Debug("Client " + Id + " Disconnected");

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
                _logger.Debug("Enter in Start method of InternalPipeServer " + Id);
                _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex);
                throw;
            }
        }

        /// <summary>
        /// This callback is called when the async WaitForConnection operation is completed,
        /// whether a connection was made or not. WaitForConnection can be completed when the server disconnects.
        /// </summary>
        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            try
            {
                _logger.Debug("Enter in WaitForConnectionCallBack method of InternalPipeServer " + Id);
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
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }


        /// <summary>
        /// This method disconnects, closes and disposes the server
        /// </summary>
        public void Stop()
        {
            try
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
                    _logger.Error("Error while disconnecting the pipe server", ex);
                    throw;
                }
                finally
                {
                    _pipeServer.Close();
                    _pipeServer.Dispose();
                    _logger.Info("Pipe server stopped");
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e);
                throw;
            }
        }



        /// <summary>
        /// This method begins an asynchronous read operation.
        /// </summary>
        private void BeginRead(BufferReading bufferReading)
        {
            try
            {
                _logger.Debug("Enter in BeginRead method of InternalPipeServer " + Id);
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
            try
            {
                _logger.Debug("Enter in EndReadCallBack method of InternalPipeServer " + Id);

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
                                _logger.Debug("No bytes read, client disconnected");
                                OnDisconnected();
                                Stop();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }


        public Task<TaskResult> SendMessage(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskResult>();

            try
            {
                _logger.Debug("Enter in SendMessage method of InternalPipeServer " + Id);
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

            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            return taskCompletionSource.Task;

        }


        public TaskResult EndWriteCallBack(IAsyncResult asyncResult)
        {
            try
            {
                _logger.Debug("Enter in EndWriteCallBack");
                _pipeServer.EndWrite(asyncResult);
                _pipeServer.Flush();
                return new TaskResult { IsSuccess = true };

            }
            catch (Exception e)
            {
                _logger.Error(e);
                return new TaskResult { IsSuccess = false };

            }

        }


        public bool isConnected()
        {
            return _pipeServer.IsConnected;
        }

        #endregion
    }
}
