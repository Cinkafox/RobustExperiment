using System.Numerics;
using Content.Shared.StackSpriting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;

namespace Content.Client.StackSpriting;

public sealed class StackSpritingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    private readonly TransformSystem _transformSystem;
    private readonly SpriteSystem _spriteSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public StackSpritingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transformSystem = _entityManager.System<TransformSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }
    
    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle; 
        var bounds = args.WorldAABB.Enlarged(5f);
        var eye = _eyeManager.CurrentEye;
        
        var query = _entityManager.EntityQueryEnumerator<StackSpriteComponent,TransformComponent>();
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);;
            if(!bounds.Contains(drawPos))
                continue;

            var rot = (drawPos - eye.Position.Position) * 0.01f + eye.Rotation.ToVec() * eye.Zoom * 0.01f;
            
            var rsi = _spriteSystem.RsiStateLike(stackSpriteComponent.Sprite);
            for (var i = rsi.AnimationFrameCount-1; i >= 0; i--)
            {
                var tex = rsi.GetFrame(RsiDirection.South,i);
                var localPos = drawPos + rot * (rsi.AnimationFrameCount - 1 - i) ;
                handle.DrawTexture(tex,localPos,transformComponent.WorldRotation);
            }
        }
    }
}