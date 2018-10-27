// The standard StreamReader will read from the underlying stream even if
// there's data in the buffer available, which will block the read
// operation. This StreamReader does not.
#define DONT_READ_WHEN_BUFFER_AVAILABLE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    internal class CustomStreamReader : TextReader
    {
        private const int DefaultBufferSize = 1024;
        private const int MinBufferSize = 128;

        private Stream _stream;
        private Decoder _decoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private int _charPos;
        private int _charLen;
        private int _byteLen;
        private readonly int _maxCharsPerBuffer;
        private bool _isBlocked;
        private readonly bool _closable;

        public CustomStreamReader(Stream stream)
            : this(stream, Encoding.UTF8, DefaultBufferSize, false)
        {
        }

        public CustomStreamReader(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen)
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException((stream == null ? "stream" : "encoding"));
            if (!stream.CanRead)
                throw new ArgumentException("Stream not readable");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive");

            _stream = stream;
            _decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize)
                bufferSize = MinBufferSize;
            _byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new char[_maxCharsPerBuffer];
            _byteLen = 0;
            _isBlocked = false;
            _closable = !leaveOpen;
        }

        public override int Peek()
        {
            throw new NotSupportedException();
        }

        public override int Read()
        {
            throw new NotSupportedException();
        }

        public override string ReadLine()
        {
            throw new NotSupportedException();
        }

        public override string ReadToEnd()
        {
            throw new NotSupportedException();
        }

        public override int Read([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), "Cannot be negative");
            if (buffer.Length - index < count)
                throw new ArgumentException("Invalid length");

            if (_stream == null)
                throw new InvalidOperationException("Reader has been closed");

            int charsRead = 0;
            bool readToUserBuffer = false;

            while (count > 0)
            {
                int n = _charLen - _charPos;
                if (n == 0)
                    n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                if (n == 0)
                    break;  // We're at EOF
                if (n > count)
                    n = count;
                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(_charBuffer, _charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    _charPos += n;
                }

                charsRead += n;
                count -= n;

                if (_isBlocked)
                    break;

#if DONT_READ_WHEN_BUFFER_AVAILABLE
                break;
#endif
            }

            return charsRead;
        }

        public override int ReadBlock([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), "Cannot be negative");
            if (buffer.Length - index < count)
                throw new ArgumentException("Invalid length");

            if (_stream == null)
                throw new InvalidOperationException("Reader has been closed");

            return base.ReadBlock(buffer, index, count);
        }

        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            _charLen = 0;
            _charPos = 0;
            _byteLen = 0;

            int charsRead = 0;
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);

                if (_byteLen == 0) // EOF
                    break;

                _isBlocked = (_byteLen < _byteBuffer.Length);

                _charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                    _charLen = 0;
                }
                else
                {
                    charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                    _charLen += charsRead;
                }
            }
            while (charsRead == 0);

            _isBlocked &= charsRead < desiredChars;

            return charsRead;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_closable && disposing)
                    _stream?.Close();
            }
            finally
            {
                if (_closable && _stream != null)
                {
                    _stream = null;
                    _decoder = null;
                    _byteBuffer = null;
                    _charBuffer = null;
                    _charPos = 0;
                    _charLen = 0;

                    base.Dispose(disposing);
                }
            }
        }
    }
}
