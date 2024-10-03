using System.Globalization;
using System.IO;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.DimensionEnv.ObjRes.MTL;

public sealed class MaterialResource : BaseResource
{
    public Dictionary<string,Material> Materials = new();
    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        var manager = dependencies.Resolve<IResourceManager>();

        using var stream = manager.ContentFileRead(path);
        using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
        {
            var mp = new MaterialParser(reader, path);
            Materials = mp.Materials;
        }
    }
}



public sealed class MaterialParser
{
    public Dictionary<string,Material> Materials = new();
    public ResPath Path;

    public MaterialParser(TextReader textReader,ResPath path)
    {
        Path = path;
        
        var line = textReader.ReadLine();
        var linecol = 0;
        while (line is not null)
        {
            try
            {
                ParseLine(line);
                line = textReader.ReadLine();
                linecol++;
            }
            catch (Exception e)
            {
                throw new Exception("WE ARE DOOMED IN LINE " + line, e);
            }
        }
        Finish();
    }

    public string currMaterial = "";
    public Material curr;

    public void ParseLine(string line)
    {
        var splited = line.Split(" ");

        var argContent = 1;

        if (splited.Length > argContent && splited[argContent] == "") argContent++;
        
        switch (splited[0].Trim())
        {
            case "newmtl":
                Materials[currMaterial] = curr;
                currMaterial = splited[argContent];
                curr = new();
                break;
            case "Kd":
                curr.Kd = new Vector3(
                    float.Parse(splited[argContent], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 1], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 2], CultureInfo.InvariantCulture.NumberFormat)
                );
                break;
            case "Ka":
                curr.Ka = new Vector3(
                    float.Parse(splited[argContent], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 1], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 2], CultureInfo.InvariantCulture.NumberFormat)
                );
                break;
            case "Ke":
                curr.Ke = new Vector3(
                    float.Parse(splited[argContent], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 1], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 2], CultureInfo.InvariantCulture.NumberFormat)
                );
                break;
            case "Ks":
                curr.Ks = new Vector3(
                    float.Parse(splited[argContent], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 1], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(splited[argContent + 2], CultureInfo.InvariantCulture.NumberFormat)
                );
                break;
            case "map_Kd":
                var localPath = splited[argContent];
                for (int i = argContent + 1; i < splited.Length; i++)
                {
                    localPath += " " + splited[i];
                }
                curr.MapKd = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(Path.Directory / localPath);
                break;
            case "map_Ka":
                var localPath1 = splited[argContent];
                for (int i = argContent + 1; i < splited.Length; i++)
                {
                    localPath1 += " " + splited[i];
                }
                curr.MapKa = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(Path.Directory / localPath1);
                break;
          
        }
        
    }

    public void Finish()
    {
        Materials[currMaterial] = curr;
    }
}


public sealed class MtlLoadContent : BaseContent
{

    public Dictionary<string, Material> Materials;
    
    public MtlLoadContent(string[] args, int count, ResPath path)
    {
        var localPath = args[count];
        for (int i = count + 1; i < args.Length; i++)
        {
            localPath += " " + args[i];
        }
        
        Materials = IoCManager.Resolve<IResourceCache>().GetResource<MaterialResource>(path / localPath).Materials;
    }
}