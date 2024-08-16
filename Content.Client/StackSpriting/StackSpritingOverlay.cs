using System.Numerics;
using Content.Shared;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Profiling;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.StackSpriting;

public sealed class StackSpritingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private readonly TransformSystem _transformSystem;
    
    private int _stackByOneLayer = 1;
    private StackSpriteAccumulator _accumulator = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public StackSpritingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _configurationManager.OnValueChanged(CCVars.StackByOneLayer,OnStackLayerChanged,true);
        _transformSystem = _entityManager.System<TransformSystem>();
    }

    private void OnStackLayerChanged(int stackByOneLayer)
    {
        _stackByOneLayer = stackByOneLayer;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = _eyeManager.CurrentEye;
        var bounds = args.WorldAABB.Enlarged(5f);
        var query = _entityManager.EntityQueryEnumerator<RendererStackSpriteComponent, TransformComponent>();
        
        using var stackHandle = new DrawingHandleStackSprite(_accumulator, args.DrawingHandle, eye, args.WorldAABB);
        using var draw = _profManager.Group("SpriteStackDraw");
        
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);;
            
            if(!bounds.Contains(drawPos))
                continue;
                
            for (var i = 0; i < stackSpriteComponent.Height; i++)
            {
                var tex = stackSpriteComponent.Layers[i];

                var textureId = stackHandle.AddTexture(tex);
                
                for (var z = 0; z < _stackByOneLayer; z++)
                {
                    var zLevelLayer = z / (float)_stackByOneLayer;
                    var texPos = new Vector3(drawPos.X, i + zLevelLayer, drawPos.Y);
                    
                    stackHandle.DrawSpriteLayer(textureId, texPos,stackSpriteComponent.Center, _transformSystem.GetWorldRotation(uid), 0, 0);
                }
            }
        }
    }
}