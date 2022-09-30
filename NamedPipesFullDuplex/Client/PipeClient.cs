using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NamedPipesFullDuplex.Server;
using log4net;
using NamedPipesFullDuplex.Interfaces;
using NamedPipesFullDuplex.Utilities;
using static NamedPipesFullDuplex.Server.InternalPipeServer;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Extensions;
using NamedPipesFullDuplex.logging;

namespace NamedPipesFullDuplex.Client
{
    public class PipeClient : IPipeClient
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PipeClient));

        private NamedPipeClientStream _pipeClient;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        private readonly SynchronizationContext _synchronizationContext;

        public PipeClient(string pipeName)
        {
            try
            {
                _logger.Debug("Enter in constructor of PipeClient ");
                _logger.Trace("Recived parameter  pipeName = " + pipeName);
                _synchronizationContext = AsyncOperationManager.SynchronizationContext;

                _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            }
            catch (Exception e)
            {

                _logger.Fatal(e);
            }
        }


        #region ICommunicationClient implementation

        /// <summary>
        /// Starts the client. Connects to the server.
        /// </summary>
        public void Start()
        {
            try
            {
                _logger.Debug("Enter in Start method of PipeClient ");

                DateTime start = DateTime.Now;

                const int tryConnectTimeout = 60 * 1000; // 1 minuto
                try
                {
                    _logger.Debug("Try to connect to server ");
                    _pipeClient.Connect(tryConnectTimeout);
                    _logger.Debug("Connected to server");

                }
                catch (Exception e)
                {
                    _logger.Fatal(e);
                }

                if (_pipeClient.IsConnected)
                {
                    BeginRead(new BufferReading());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        /// <summary>
        /// Stops the client. Waits for pipe drain, closes and disposes it.
        /// </summary>
        public void Stop()
        {
            try
            {
                _logger.Debug("Enter in Stop method of PipeClient ");
                _pipeClient.WaitForPipeDrain();
            }
            catch (Exception e)

            {
                _logger.Error(e);
            }
            finally
            {

                _pipeClient.Close();
                _pipeClient.Dispose();
            }
        }

        #endregion

        #region event

        /// <summary>
        /// This method fires MessageReceivedEvent with the given message
        /// </summary>
        private void OnMessageReceived(string message)
        {
            try
            {
                _logger.Info("New message recived : " + message);

                MessageReceivedEventArgs args = new MessageReceivedEventArgs { Message = message };
                _synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), args);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        #endregion

        #region  methods

        /// <summary>
        /// This method begins an asynchronous read operation.
        /// </summary>
        private void BeginRead(BufferReading bufferReading)
        {
            try
            {
                _logger.Debug("Enter in BeginRead method of PipeClient ");

                _pipeClient.BeginRead(bufferReading.Buffer, 0, bufferReading.BufferSize, EndReadCallBack, bufferReading);
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
                _logger.Debug("Enter in EndReadCallBack method");

                var readBytes = _pipeClient.EndRead(result);
                if (readBytes > 0)
                {
                    var info = (BufferReading)result.AsyncState;

                    // Get the read bytes and append them
                    info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                    var message = info.StringBuilder.ToString().TrimEnd('\0');

                    OnMessageReceived(message);

                    // Begin a new reading operation
                    BeginRead(new BufferReading());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }


        public Task<TaskResult> SendMessage(string message)
        {
            try
            {
                _logger.Debug("Enter in SendMessage method of PipeClient ");
                _logger.Trace("Recived parameter  message = " + message);

                var taskCompletionSource = new TaskCompletionSource<TaskResult>();

                if (_pipeClient.IsConnected)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    _pipeClient.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
                    {
                        try
                        {
                            taskCompletionSource.SetResult(EndSendMessageCallBack(asyncResult));
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
            catch (Exception e)

            {
                _logger.Error(e);
                throw;
            }
        }

        /// <summary>
        /// This callback is called when the BeginWrite operation is completed.
        /// It can be called whether the connection is valid or not.
        /// </summary>
        /// <param name="asyncResult"></param>
        public TaskResult EndSendMessageCallBack(IAsyncResult asyncResult)
        {
            try
            {
                _pipeClient.EndWrite(asyncResult);
                _pipeClient.Flush();
                return new TaskResult { IsSuccess = true };
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }

        #endregion
    }
}
