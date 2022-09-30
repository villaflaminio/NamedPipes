using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientServerUsingNamedPipes.Interfaces;
using ClientServerUsingNamedPipes.Server;
using ClientServerUsingNamedPipes.Utilities;
using static ClientServerUsingNamedPipes.Server.InternalPipeServer;

namespace ClientServerUsingNamedPipes.Client
{
    public class PipeClient : IPipeClient
    {
        private NamedPipeClientStream _pipeClient;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        private readonly SynchronizationContext _synchronizationContext;

        public PipeClient(string pipeName)
        {
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;

            _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }


        #region ICommunicationClient implementation

        /// <summary>
        /// Starts the client. Connects to the server.
        /// </summary>
        public void Start()
        {
            DateTime start = DateTime.Now;

            const int tryConnectTimeout = 60 * 1000; // 1 minuto
            try
            {
                _pipeClient.Connect(tryConnectTimeout);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (_pipeClient.IsConnected)
            {
                BeginRead(new BufferReading());
            }

        }

        /// <summary>
        /// Stops the client. Waits for pipe drain, closes and disposes it.
        /// </summary>
        public void Stop()
        {
            try
            {
                _pipeClient.WaitForPipeDrain();
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

            MessageReceivedEventArgs args = new MessageReceivedEventArgs { Message = message };
            _synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs)e), args);
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
                _pipeClient.BeginRead(bufferReading.Buffer, 0, bufferReading.BufferSize, EndReadCallBack, bufferReading);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// This callback is called when the BeginRead operation is completed.
        /// We can arrive here whether the connection is valid or not
        /// </summary>
        private void EndReadCallBack(IAsyncResult result)
        {
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
                //}
            }
        }


        public Task<TaskResult> SendMessage(string message)
        {
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
                Logger.Error("Cannot send message, pipe is not connected");
                throw new IOException("pipe is not connected");
            }

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// This callback is called when the BeginWrite operation is completed.
        /// It can be called whether the connection is valid or not.
        /// </summary>
        /// <param name="asyncResult"></param>
        public TaskResult EndSendMessageCallBack(IAsyncResult asyncResult)
        {
            _pipeClient.EndWrite(asyncResult);
            _pipeClient.Flush();

            return new TaskResult { IsSuccess = true };
        }

        #endregion
    }
}
