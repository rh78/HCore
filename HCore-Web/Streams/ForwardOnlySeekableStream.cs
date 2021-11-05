using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Web.Streams
{
    public class ForwardOnlySeekableStream : Stream
    {
        private readonly Stream _inner;
        private long _bytesRemaining;
        private long _contentLength;
        private bool _disposed;

        public ForwardOnlySeekableStream(Stream inner, long contentLength)
        {
            _inner = inner;
            _bytesRemaining = contentLength;
            _contentLength = contentLength;
        }

        public override bool CanRead
        {
            get { return !_disposed; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanTimeout
        {
            get { return _inner.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _contentLength; }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int ReadTimeout
        {
            get
            {
                CheckDisposed();
                return _inner.ReadTimeout;
            }
            set
            {
                CheckDisposed();
                _inner.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                CheckDisposed();
                return _inner.WriteTimeout;
            }
            set
            {
                CheckDisposed();
                _inner.WriteTimeout = value;
            }
        }

        private void UpdateBytesRemaining(int read)
        {
            _bytesRemaining -= read;
            if (_bytesRemaining <= 0)
            {
                _disposed = true;
            }
            System.Diagnostics.Debug.Assert(_bytesRemaining >= 0, "Negative bytes remaining? " + _bytesRemaining);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                return 0;
            }

            int toRead = (int)Math.Min(count, _bytesRemaining);
            int read = _inner.Read(buffer, offset, toRead);

            UpdateBytesRemaining(read);

            return read;
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return 0;
            }

            cancellationToken.ThrowIfCancellationRequested();

            int toRead = (int)Math.Min(count, _bytesRemaining);
            int read = await _inner.ReadAsync(buffer, offset, toRead, cancellationToken);

            UpdateBytesRemaining(read);

            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(ForwardOnlySeekableStream).FullName);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset == 0)
                return 0;

            if (offset < 0)
            {
                throw new Exception("Backwords seeking is not supported");
            }

            byte[] buffer = new byte[65536];
             
            int totalRead = 0;
            int read;

            int bytesToRead = (int)offset;

            while ((read = _inner.Read(buffer, 0, bytesToRead > buffer.Length ? buffer.Length : bytesToRead)) > 0)
            {
                bytesToRead -= read;
                totalRead += read;

                if (bytesToRead == 0)
                    break;
            }

            UpdateBytesRemaining(totalRead);

            if (totalRead < offset)
                throw new EndOfStreamException();

            return totalRead;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }
    }
}
