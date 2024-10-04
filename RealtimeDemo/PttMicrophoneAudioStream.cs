using NAudio.Wave;
using System.Runtime.InteropServices;

#nullable disable

public class PttMicrophoneAudioStream : Stream, IDisposable
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_SHIFT = 0x10;

    private const int SAMPLES_PER_SECOND = 24000;
    private const int BYTES_PER_SAMPLE = 2;
    private const int CHANNELS = 1;

    // For simplicity, this is configured to use a static 10-second ring buffer.
    private readonly byte[] _buffer = new byte[BYTES_PER_SAMPLE * SAMPLES_PER_SECOND * CHANNELS * 10];
    private readonly object _bufferLock = new();
    private int _bufferReadPos = 0;
    private int _bufferWritePos = 0;

    private readonly WaveInEvent _waveInEvent;

    private bool isRecording = false;

    private PttMicrophoneAudioStream()
    {
        _waveInEvent = new()
        {
            WaveFormat = new WaveFormat(SAMPLES_PER_SECOND, BYTES_PER_SAMPLE * 8, CHANNELS),
        };
        _waveInEvent.DataAvailable += (_, e) =>
        {
            bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

            if (shiftPressed)
            {
                if (!isRecording)
                {
                    isRecording = true;
                    Console.WriteLine("Recording started");
                }

                lock (_bufferLock)
                {
                    int bytesToCopy = e.BytesRecorded;
                    int bytesCopied = 0;

                    while (bytesToCopy > 0)
                    {
                        int spaceAtEnd = _buffer.Length - _bufferWritePos;
                        int bytesToWrite = Math.Min(bytesToCopy, spaceAtEnd);

                        Array.Copy(e.Buffer, bytesCopied, _buffer, _bufferWritePos, bytesToWrite);

                        _bufferWritePos = (_bufferWritePos + bytesToWrite) % _buffer.Length;
                        bytesToCopy -= bytesToWrite;
                        bytesCopied += bytesToWrite;
                    }
                }
            }
            else
            {
                if (isRecording)
                {
                    isRecording = false;
                    Console.WriteLine("Recording stopped");
                }
                // Do not copy data
            }
        };
        _waveInEvent.StartRecording();
    }

    public static PttMicrophoneAudioStream Start() => new();

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalCount = count;

        int GetBytesAvailable()
        {
            lock (_bufferLock)
            {
                return _bufferWritePos < _bufferReadPos
                    ? _bufferWritePos + (_buffer.Length - _bufferReadPos)
                    : _bufferWritePos - _bufferReadPos;
            }
        }

        // For simplicity, we'll block until all requested data is available and not perform partial reads.
        while (GetBytesAvailable() < count)
        {
            Thread.Sleep(100);
        }

        lock (_bufferLock)
        {
            int bytesCopied = 0;

            while (count > 0)
            {
                int bytesToEnd = _buffer.Length - _bufferReadPos;
                int bytesToRead = Math.Min(count, bytesToEnd);

                Array.Copy(_buffer, _bufferReadPos, buffer, offset + bytesCopied, bytesToRead);

                _bufferReadPos = (_bufferReadPos + bytesToRead) % _buffer.Length;
                count -= bytesToRead;
                bytesCopied += bytesToRead;
            }

            return totalCount;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        _waveInEvent?.Dispose();
        base.Dispose(disposing);
    }
}
