namespace Content.Shared.IoC;


public interface IInitializeBehavior
{
    public void Initialize();
}
public interface IPostInitializeBehavior
{
    public void PostInitialize();
}