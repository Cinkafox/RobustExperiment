namespace Content.Shared.Thread;

public sealed class ThreadPool<T> where T: class
{
    private readonly ThreadPoolItem[] _buffer;
    private readonly HashSet<int> _taken = new();
    private readonly IObjectCreator<T> _creator;
    
    public ThreadPool(int length, IObjectCreator<T> creator)
    {
        _buffer = new ThreadPoolItem[length];
        _creator = creator;
    }

    public ThreadPoolItem Take()
    {
        lock (_buffer)
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                if(_taken.Contains(i)) continue;

                if (_buffer[i] == default)
                {
                    _buffer[i] = new ThreadPoolItem(this, _creator.Create(), i);
                }
                
                lock (_taken)
                {
                    _taken.Add(i);
                }

                return _buffer[i];
            }
        }

        return default!;
    }

    public void Free(int index)
    {
        lock (_taken)
        {
            _taken.Remove(index);
        }
    }

    public void DisposeAll()
    {
        lock (_taken)
        {
            _taken.Clear();
        }
    }
    
    public sealed class ThreadPoolItem : IDisposable
    {
        private readonly ThreadPool<T> _parent;
        public T Value { get; }
        public int Index { get; }

        internal ThreadPoolItem(ThreadPool<T> parent, T value, int index)
        {
            _parent = parent;
            Value = value;
            Index = index;
        }

        public void Dispose()
        {
            _parent.Free(Index);
        }
    }
}