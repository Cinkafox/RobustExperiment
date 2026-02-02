namespace Content.Client.DimensionEnv.ObjRes.Content;

public sealed class FaceContent : BaseContent
{
    public Face Face;

    public FaceContent(string[] args, int argStart)
    {
        var len = args.Length - argStart;
        if (len < 3)
            throw new Exception($"Error while parsing face! Count of dots less that 3. Current value: {len}");
        Face = new Face(len);
        
        for (int i = argStart; i < args.Length; i++)
        {
            var splited = args[i].Split("/");
            switch (splited.Length)
            {
                case 1:
                    Face.Vertices[i - 1] = new FaceVertex(Parse(splited[0]) - 1);
                    break;
                case 2:
                    Face.Vertices[i - 1] = new FaceVertex(Parse(splited[0]) - 1, Parse(splited[1]));
                    Face.HasTexturePos = true;
                    break;
                case 3:
                    Face.Vertices[i - 1] = new FaceVertex(Parse(splited[0]) - 1, Parse(splited[1]),Parse(splited[2]) - 1);
                    Face.HasNormal = true;
                    Face.HasTexturePos = true;
                    break;
                default: throw new Exception();
            }
        }
        
    }

    public int Parse(string arg)
    {
        var value = int.Parse(arg);
        if (value <= 0) throw new Exception("Error while parsing face.");
        return value;
    }
}

public sealed class Face
{
    public FaceVertex[] Vertices;
    
    public int MaterialId;
    
    public bool HasTexturePos;
    public bool HasNormal;

    public Face(FaceVertex[] vertices)
    {
        Vertices = vertices;
        HasTexturePos = vertices[0].TexPosId != -1;
        HasNormal = vertices[0].NormalId != -1;
    }

    public Face(int length)
    {
        Vertices = new FaceVertex[length];
    }
}

public struct FaceVertex
{
    public int VertexId;
    public int TexPosId;
    public int NormalId;

    public FaceVertex(int vertexId, int texPosId = -1, int normalId = -1)
    {
        VertexId = vertexId;
        TexPosId = texPosId;
        NormalId = normalId;
    }
}