using Content.Shared.Thread;
using Robust.Client.Graphics;

namespace Content.Client.Viewport;

public sealed class ShaderCreator : IObjectCreator<ShaderInstance>
{
    private readonly ShaderInstance _original;

    public ShaderCreator(ShaderInstance original)
    {
        _original = original;
    }
    
    public ShaderInstance Create()
    {
        return _original.Duplicate();
    }
}