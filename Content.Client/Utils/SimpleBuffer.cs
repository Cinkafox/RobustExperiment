using System.Collections;

namespace Content.Client.Utils;

public sealed class SimpleBuffer<T> : IEnumerable<T>
{
    public readonly T[] Buffer;
    
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
        Buffer = new T[limit];
        Limit = limit;
    }

    public void Add(T obj)
    {
        Buffer[Shift + Length] = obj;
        Length++;
    }

    public T Pop()
    {
        var obj = Buffer[Shift + Length];

        Shift++;
        Length--;

        return obj;
    }

    public T Get(int pos)
    {
        return Buffer[Shift+pos];
    }

    public void Set(int pos, T obj)
    {
        Buffer[Shift+pos] = obj;
    }

    public void Clear()
    {
        Length = 0;
        Shift = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Length; i++)
        {
            yield return Buffer[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}