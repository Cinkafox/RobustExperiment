using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client.ContentCVar;

[CVarDefs]
public sealed class CCVars : CVars
{
    public static readonly CVarDef<int> StackByOneLayer = CVarDef.Create("stacksprite.layer_count", 10, CVar.CLIENT);
}