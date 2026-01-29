using System.Collections;
using Robust.Shared.Sandboxing;

namespace Content.Client.Utils;

public sealed class SimpleBuffer<T> : IEnumerable<T>
{
    public readonly T[] Buffer;
    public bool IsInLimit => Length == Limit;
    public readonly int Limit;
    public int Length { get; set; }
    public int Shift { get; private set; }

    public T this[int pos]
    {
        get => Get(pos);
        set => Set(pos, value);
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
        var obj = Buffer[Shift];
        Shift++;
        Length--;
        return obj;
    }

    public T Get(int pos)
    {
        return Buffer[Shift + pos];
    }

    public void Set(int pos, T obj)
    {
        Buffer[Shift + pos] = obj;
    }
    
    public T Take()
    {
        var realId = Shift + Length;
        var objectSelected = Buffer[realId];

        if (objectSelected == null)
        {
            objectSelected = (T)IoCManager.Resolve<ISandboxHelper>().CreateInstance(typeof(T));
            Buffer[realId] = objectSelected;
        }
        
        Length++;
        return objectSelected;
    }

    public void Clear()
    {
        Length = 0;
        Shift = 0;
    }
    
    public void Sort(IComparer<T>? comparer = null)
    {
        Array.Sort(Buffer, Shift, Length, comparer);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Length; i++)
        {
            yield return Buffer[Shift + i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
