namespace Content.Shared.Utils;

public static class DictionaryExtensions
{
    public static void RemoveAll<TKey, TValue>(
        this Dictionary<TKey, TValue> dict,
        Predicate<KeyValuePair<TKey, TValue>> pred)
        where TKey : notnull
    {
        var remove = new List<TKey>();

        foreach (var kv in dict)
        {
            if (pred(kv))
                remove.Add(kv.Key);
        }

        foreach (var key in remove)
        {
            dict.Remove(key);
        }
    }
}