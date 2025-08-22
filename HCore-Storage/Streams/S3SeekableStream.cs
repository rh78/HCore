using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Storage.Streams
{
    public class S3SeekableStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _contentLength;
        private long _position = 0;

        public S3SeekableStream(Stream inner, long contentLength)
        {
            ArgumentNullException.ThrowIfNull(inner);

            _inner = inner;
            _contentLength = contentLength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _contentLength;

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                _ => throw new NotSupportedException()
            };

            if (newPosition < _position)
            {
                throw new NotSupportedException("Backward seeking is not supported");
            }

            _position = newPosition;

            return _position;
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
        }

        public override ValueTask DisposeAsync()
        {
            return _inner.DisposeAsync();
        }
    }
}
