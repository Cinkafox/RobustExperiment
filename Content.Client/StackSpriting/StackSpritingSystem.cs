using Content.Shared.StackSpriting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.StackSpriting;

public sealed class StackSpritingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    
    public override void Initialize()
    {
        Logger.Debug("REG OVERLAY");
        _overlayManager.AddOverlay(new StackSpritingOverlay());
    }
    
}