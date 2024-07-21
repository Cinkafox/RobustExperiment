using Content.Shared.StackSpriting;
using Robust.Client.GameObjects;
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
        renderer.Texture = texture;
        renderer.Height = count;
        renderer.Center = stackSpriteComponent.Center;
    }
}