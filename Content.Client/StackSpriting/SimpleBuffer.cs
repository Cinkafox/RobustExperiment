using System.Collections;

namespace Content.Client.StackSpriting;

public sealed class SimpleBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    
    public readonly int Limit;
    public int Length;
    public int Shift;
    public T this[int pos]
    {
        get => Get(pos);
        set => Set(pos,value);
    }

    public SimpleBuffer(int limit)
    {
        _buffer = new T[limit];
        Limit = limit;
    }

    public void Add(T obj)
    {
        if (Shift + Length >= Limit)
            throw new Exception($"{Shift + Length} reached the limit {Limit}");
        
        _buffer[Shift + Length] = obj;
        Length++;
    }

    public T Pop()
    {
        var obj = _buffer[Shift + Length];

        Shift++;
        Length--;

        return obj;
    }

    public T Get(int pos)
    {
        if (Shift + pos >= Limit)
            throw new Exception($"{Shift + pos} reached the limit {Limit}");
        return _buffer[Shift+pos];
    }

    public void Set(int pos, T obj)
    {
        if (Shift + pos >= Limit)
            throw new Exception($"{Shift + pos} reached the limit {Limit}");
        _buffer[Shift+pos] = obj;
    }

    public void Clear()
    {
        Length = 0;
        Shift = 0;
    }

    public T[] ToArray()
    {
        return _buffer;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var data in _buffer)
        {
            yield return data;
        } 
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _buffer.GetEnumerator();
    }
}