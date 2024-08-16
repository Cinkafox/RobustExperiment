using System.Numerics;
using Content.Shared.StackSpriting;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.StackSpriting;

public sealed class StackSpritingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    
    public override void Initialize()
    {
        _overlayManager.AddOverlay(new StackSpritingOverlay());
        SubscribeLocalEvent<StackSpriteComponent,ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, StackSpriteComponent stackSpriteComponent, ref ComponentInit args)
    {
        var texture = _resourceCache.GetResource<TextureResource>(stackSpriteComponent.Path).Texture;
        var count = texture.Width / stackSpriteComponent.Size.X * texture.Height / stackSpriteComponent.Size.Y;
        var renderer = EnsureComp<RendererStackSpriteComponent>(uid);
        
        renderer.Size = stackSpriteComponent.Size;
        renderer.Height = count;
        renderer.Center = stackSpriteComponent.Center;
        
        ExtractTextures(renderer, texture);
    }

    private void ExtractTextures(RendererStackSpriteComponent stackSpriteComponent, Robust.Client.Graphics.Texture mainTexture)
    {
        var layerSize = stackSpriteComponent.Size;
        
        var cols = mainTexture.Width / layerSize.X;
        var rows = mainTexture.Height / layerSize.Y;
        
        var totalLayers = cols * rows;
        
        for (var i = 0; i < totalLayers; i++)
        {
            var x = (i % cols) * layerSize.X;
            var y = (i / cols) * layerSize.Y;

            var tex = new AtlasTexture(mainTexture,
                UIBox2.FromDimensions(new Vector2(x, y), layerSize));

            stackSpriteComponent.Layers.Add(tex);
        }
    }
}