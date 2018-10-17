using Amqp;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Amqp.Processor.Hosts
{
    internal abstract class LinkHost
    {
        private readonly string _connectionString;

        protected string Address { get; }

        private readonly ConnectionFactory _connectionFactory;
        private Connection _connection;
        private Session _session;

        protected readonly CancellationToken CancellationToken;

        protected LinkHost(ConnectionFactory connectionFactory, string connectionString, string address, CancellationToken cancellationToken)
        {
            _connectionFactory = connectionFactory;
            _connectionString = connectionString;

            Address = address;

            CancellationToken = cancellationToken;
        }

        public async Task InitializeAsync()
        {
            await CloseAsync().ConfigureAwait(false);

            _connection = await _connectionFactory.CreateAsync(new Address(_connectionString)).ConfigureAwait(false);
            _session = new Session(_connection);

            InitializeLink(_session);            
        }

        public virtual async Task CloseAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync().ConfigureAwait(false);

                _connection = null;
                _session = null;                
            }
        }

        protected abstract void InitializeLink(Session session);
    }
}
