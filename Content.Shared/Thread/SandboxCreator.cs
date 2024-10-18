using Robust.Shared.Sandboxing;

namespace Content.Shared.Thread;

public sealed class SandboxCreator<T> : IObjectCreator<T>
{
    private readonly ISandboxHelper _sandboxHelper;
    public SandboxCreator()
    {
        _sandboxHelper = IoCManager.Resolve<ISandboxHelper>();
    }

    public T Create()
    {
        return (T)_sandboxHelper.CreateInstance(typeof(T));
    }
}