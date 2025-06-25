namespace Api.Streams;

/// <summary>
/// A stream wrapper that ensures a given lock is released when the stream is disposed.
/// </summary>
/// <seealso cref="Stream"/>
/// <seealso cref="SemaphoreSlim"/>
public sealed class LockReleasingStream(Stream inner, SemaphoreSlim streamLock) : Stream
{
    private bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            inner.Dispose();
            streamLock.Release();
        }
        _disposed = true;
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        Dispose(true);
        return ValueTask.CompletedTask;
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }
    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void SetLength(long value) => inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
}
