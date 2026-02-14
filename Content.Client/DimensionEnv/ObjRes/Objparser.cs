using System.IO;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Robust.Shared.Utility;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class Objparser
{
    public List<BaseContent> Contents = new();
    public ResPath Path;
    
    private readonly IDependencyCollection _dependencyCollection;

    public Objparser(IDependencyCollection dependencyCollection, TextReader textReader, ResPath path)
    {
        _dependencyCollection = dependencyCollection;
        
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
                throw new Exception("Exception was thrown in line " + line, e);
            }
        }
    }

    public void ParseLine(string line)
    {
        var splited = line.Split(" ");

        var argContent = 1;

        if (splited.Length > argContent && splited[argContent] == "") argContent++;
        
        switch (splited[0])
        {
            case "v":
                Contents.Add(new VertexContent(splited, argContent));
                break;
            case "f":
                Contents.Add(new FaceContent(splited, argContent));
                break;
            case "vt":
                Contents.Add(new TexturePosContent(splited, argContent));
                break;
            case "vn":
                Contents.Add(new NormalContent(splited, argContent));
                break;
            case "mtllib":
                Contents.Add(new MtlLoadContent(_dependencyCollection, splited, argContent, Path));
                break;
            case "usemtl":
                Contents.Add(new MaterialContent(splited, argContent));
                break;
        }
    }
}