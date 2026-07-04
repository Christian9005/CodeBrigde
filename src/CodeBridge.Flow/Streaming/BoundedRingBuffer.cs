namespace CodeBridge.Flow.Streaming;

/// <summary>
/// Fixed-size buffer for high-rate board samples.
/// </summary>
public sealed class BoundedRingBuffer<T>
{
    private readonly T[] _items;
    private readonly object _sync = new();
    private int _head;
    private int _count;

    public BoundedRingBuffer(int capacity, BackpressurePolicy backpressure = BackpressurePolicy.DropOldest)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        if (backpressure is BackpressurePolicy.Aggregate or BackpressurePolicy.Pause)
            throw new ArgumentException("This buffer supports DropOldest and DropNewest policies.", nameof(backpressure));

        _items = new T[capacity];
        Backpressure = backpressure;
    }

    public int Capacity => _items.Length;
    public BackpressurePolicy Backpressure { get; }
    public long DroppedSamples { get; private set; }

    public int Count
    {
        get
        {
            lock (_sync)
                return _count;
        }
    }

    public bool TryWrite(T item)
    {
        lock (_sync)
        {
            if (_count == _items.Length && Backpressure == BackpressurePolicy.DropNewest)
            {
                DroppedSamples++;
                return false;
            }

            var index = (_head + _count) % _items.Length;
            if (_count == _items.Length)
            {
                _items[index] = item;
                _head = (_head + 1) % _items.Length;
                DroppedSamples++;
                return true;
            }

            _items[index] = item;
            _count++;
            return true;
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        lock (_sync)
        {
            var snapshot = new T[_count];
            for (var offset = 0; offset < _count; offset++)
            {
                var index = (_head + offset) % _items.Length;
                snapshot[offset] = _items[index];
            }

            return snapshot;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            Array.Clear(_items);
            _head = 0;
            _count = 0;
            DroppedSamples = 0;
        }
    }
}
