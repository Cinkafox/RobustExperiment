using System.Collections;

namespace Content.Client.Utils;

public sealed class SimpleBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    public int Length { get; private set; }

    public T this[int pos]
    {
        get => Get(pos);
        set => Set(pos, value);
    }

    public SimpleBuffer(int limit)
    {
        _buffer = new T[limit];
    }

    public void Add(T obj)
    {
        _buffer[Length] = obj;
        Length++;
    }
    
    public T Get(int pos)
    {
        return _buffer[pos];
    }

    public void Set(int pos, T obj)
    {
        _buffer[pos] = obj;
    }

    public void Clear()
    {
        Length = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Length; i++)
        {
            yield return _buffer[i];
        }
    }
    
    public void Sort(IComparer<T>? comparer = null)
    {
        Array.Sort(_buffer, 0, Length, comparer);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


public sealed class SimplePool<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    public int Length { get; private set; }
    
    private readonly Func<T> _factory;
    
    public SimplePool(int limit, Func<T> factory, bool init = false)
    {
        _buffer = new T[limit];
        _factory = factory;

        if(!init) return;
        
        for (var i = 0; i < limit; i++)
        {
            _buffer[i] = factory();
        }
    }
    
    public void Add(T obj)
    {
        _buffer[Length] = obj;
        Length++;
    }
    
    public T Take()
    {
        var objectSelected = _buffer[Length];

        if (objectSelected == null)
        {
            objectSelected = _factory();
            _buffer[Length] = objectSelected;
        }
        
        Length++;
        return objectSelected;
    }

    public void Clear()
    {
        Length = 0;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Length; i++)
        {
            yield return _buffer[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}