namespace Content.Shared.Thread;

public interface IObjectCreator<T>
{
    public T Create();
}