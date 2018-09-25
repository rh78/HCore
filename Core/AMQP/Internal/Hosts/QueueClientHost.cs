using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using ReinhardHolzner.Core.AMQP.Internal.Impl;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Internal.Hosts
{
    internal class QueueClientHost<TMessage>
    {
        private string _connectionString;
        private int _amqpListenerCount;

        protected string Address { get; set; }

        private QueueClient _queueClient;

        private ServiceBusMessengerImpl<TMessage> _messenger;

        protected CancellationToken CancellationToken;

        public QueueClientHost(string connectionString, int amqpListenerCount, string address, ServiceBusMessengerImpl<TMessage> messenger, CancellationToken cancellationToken)
        {
            _connectionString = connectionString;
            _amqpListenerCount = amqpListenerCount;

            Address = address;

            _messenger = messenger;

            CancellationToken = cancellationToken;
        }

        public async Task InitializeAsync()
        {
            await CloseAsync().ConfigureAwait(false);

            _queueClient = new QueueClient(_connectionString, Address);

            _queueClient.RegisterMessageHandler(ProcessMessageAsync, new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _amqpListenerCount,
                AutoComplete = false,
                MaxAutoRenewDuration = TimeSpan.MaxValue                 
            });            
        }

        private async Task ProcessMessageAsync(Message message, CancellationToken token)
        {
            try
            {
                if (!string.Equals(message.ContentType, "application/json"))
                    throw new Exception($"Invalid content type for AMQP message: {message.ContentType}");

                TMessage messageBody = JsonConvert.DeserializeObject<TMessage>(Encoding.UTF8.GetString(message.Body));
                
                await _messenger.ProcessMessageAsync(Address, messageBody).ConfigureAwait(false);

                await _queueClient.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            } catch (Exception e)
            {
                Console.WriteLine($"Exception during processing AMQP message, rejecting: {e}");

                await _queueClient.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"AMQP message handler encountered an exception: {exceptionReceivedEventArgs.Exception}");

            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            Console.WriteLine("Exception context for troubleshooting:");

            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");

            return Task.CompletedTask;
        }

        public virtual async Task CloseAsync()
        {
            if (_queueClient != null)
            {
                await _queueClient.CloseAsync();

                _queueClient = null;
            }
        }

        public async Task SendMessageAsync(TMessage messageBody)
        {
            try
            {
                if (CancellationToken.IsCancellationRequested)
                    throw new Exception("AMQP cancellation is requested, can not send message");

                if (_queueClient == null || _queueClient.IsClosedOrClosing)
                    await InitializeAsync().ConfigureAwait(false);

                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)))
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString()
                };

                await _queueClient.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (!CancellationToken.IsCancellationRequested)
                    Console.WriteLine($"AMQP exception in sender link for address {Address}: {e}");

                await CloseAsync().ConfigureAwait(false);

                if (!CancellationToken.IsCancellationRequested)
                    await SendMessageAsync(messageBody).ConfigureAwait(false);
            }
        }        
    }
}
