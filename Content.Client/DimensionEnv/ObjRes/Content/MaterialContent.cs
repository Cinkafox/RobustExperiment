namespace Content.Client.DimensionEnv.ObjRes;

public sealed class MaterialContent : BaseContent
{
    public string Material;

    public MaterialContent(string[] args)
    {
        Material = args[1];
    }
}