namespace Content.Client.DimensionEnv.ObjRes.Content;

public sealed class FaceContent : BaseContent
{
    public int[] Vertex;
    public int[] TexPos;
    public int[] Normal;
    
    public bool HasVertex;
    public bool HasTexturePos;
    public bool HasNormal;

    public FaceContent(string[] args, int argStart)
    {
        var len = args.Length - argStart;
        if (len < 3)
            throw new Exception("FUCKING BITCH! "+args.Length + " " + argStart);
        Vertex = new int[len];
        TexPos = new int[len];
        Normal = new int[len];
        
        for (int i = argStart; i < args.Length; i++)
        {
            var splited = args[i].Split("/");
            switch (splited.Length)
            {
                case 1:
                    Vertex[i - 1] = Parse(splited[0]);
                    HasVertex = true;
                    break;
                case 2:
                    TexPos[i - 1] = Parse(splited[1]);
                    Vertex[i - 1] = Parse(splited[0]);
                    HasTexturePos = true;
                    HasVertex = true;
                    break;
                case 3:
                    Normal[i - 1] = Parse(splited[2]);
                    TexPos[i - 1] = Parse(splited[1]);
                    Vertex[i - 1] = Parse(splited[0]);
                    HasNormal = true;
                    HasTexturePos = true;
                    HasVertex = true;
                    break;
            }
        }
    }

    public int Parse(string arg)
    {
        var value = int.Parse(arg);
        if (value <= 0) throw new Exception("FUCKCKCKFF");
        return value;
    }
}