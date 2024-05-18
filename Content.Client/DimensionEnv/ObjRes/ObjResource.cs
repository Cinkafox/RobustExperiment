using System.IO;
using System.Numerics;
using Content.Client.Viewport;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class ObjResource : BaseResource
{

    public Mesh Mesh = default!;
    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        var manager = dependencies.Resolve<IResourceManager>();

        using var stream = manager.ContentFileRead(path);
        using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
        {
            Mesh = Mesh.Parse(reader);
        }
    }
}

public sealed class SaObject
{
    public Mesh Mesh;
    public Texture Texture;

    public SaObject(Mesh mesh, Texture texture)
    {
        Mesh = mesh;
        Texture = texture;
    }

    public void Draw(DrawingHandle3d handle)
    {
        if (Mesh.Transform.HasValue)
        {
            for (int i = 0; i < Mesh.Vertexes.Count; i++)
            {
                Mesh.Vertexes[i] = Vector3.Transform(Mesh.Vertexes[i], Mesh.Transform.Value);
            }
        }
        
        foreach (var face in Mesh.Faces)
        {
            var v1 = Mesh.Vertexes[face.Vertex[0]-1];
            var v2 = Mesh.Vertexes[face.Vertex[1]-1];
            var v3 = Mesh.Vertexes[face.Vertex[2]-1];

            Triangle triangle;

            Vector3? normal;

            if (face.HasNormal)
            {
                var n1 = Mesh.Normals[face.Normal[0]-1];
                var n2 = Mesh.Normals[face.Normal[1]-1];
                var n3 = Mesh.Normals[face.Normal[2]-1];
            }

            if (face.HasTexturePos)
            {
                var t1 = Mesh.TextureCoords[face.TexPos[0] - 1];
                var t2 = Mesh.TextureCoords[face.TexPos[1] - 1];
                var t3 = Mesh.TextureCoords[face.TexPos[2] - 1];

                triangle = new Triangle(new DrawVertexUV3D(v1, t1), new DrawVertexUV3D(v2, t2),
                    new DrawVertexUV3D(v3, t3), Texture.White);

            }
            else 
                triangle = new Triangle(v1, v2, v3);
            
            handle.DrawPolygon(triangle);
        }

        Mesh.Transform = null;
    }
}

public sealed class Mesh
{
    public List<Vector3> Vertexes = new();
    public List<Vector3> Normals = new();
    public List<FaceContent> Faces = new();
    public List<Vector2> TextureCoords = new();
    public Matrix4? Transform;

    public void ApplyTransform(Matrix4 matrix4)
    {
        for (int i = 0; i < Vertexes.Count; i++)
        {
            Vertexes[i] = Vector3.Transform(Vertexes[i], matrix4);
        }
    }

    public static Mesh Parse(TextReader textReader)
    {
        var mesh = new Mesh();
        var parser = new MeshParser(textReader);

        foreach (var content in parser.Contents)
        {
            if (content is VertexContent vertexContent)
            {
                mesh.Vertexes.Add(vertexContent.Vertex);
            }

            if (content is FaceContent faceContent)
            {
                mesh.Faces.Add(faceContent);
            }

            if (content is TexturePosContent texturePosContent)
            {
                mesh.TextureCoords.Add(texturePosContent.TexturePos);
            }

            if (content is NormalContent normalContent)
            {
                mesh.Normals.Add(normalContent.Normal);
            }
        }


        return mesh;
    }
}

public sealed class MeshParser
{
    public List<BaseContent> Contents = new();

    public MeshParser(TextReader textReader)
    {
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
    }

    public void ParseLine(string line)
    {
        var splited = line.Split(" ");
        
        switch (splited[0])
        {
            case "v":
                Contents.Add(new VertexContent(splited));
                break;
            case "f":
                Contents.Add(new FaceContent(splited));
                break;
            case "vt":
                Contents.Add(new TexturePosContent(splited));
                break;
            case "vn":
                Contents.Add(new NormalContent(splited));
                break;
        }
    }
}

public abstract class BaseContent
{
    
}

public sealed class VertexContent : BaseContent
{
    public Vector3 Vertex;

    public VertexContent(string[] args)
    {
        Vertex = new Vector3(
            float.Parse(args[1].Replace(".",",")),
            float.Parse(args[2].Replace(".",",")),
            float.Parse(args[3].Replace(".",","))
        );
    }
}

public sealed class FaceContent : BaseContent
{
    public int[] Vertex = new int[3];
    public int[] TexPos = new int[3];
    public int[] Normal = new int[3];
    
    public bool HasVertex;
    public bool HasTexturePos;
    public bool HasNormal;

    public FaceContent(string[] args)
    {
        for (int i = 1; i < 4; i++)
        {
            var splited = args[i].Split("/");

            switch (splited.Length)
            {
                case 1:
                    Vertex[i - 1] = int.Parse(splited[0]);
                    HasVertex = true;
                    break;
                case 2:
                    TexPos[i - 1] = int.Parse(splited[1]);
                    Vertex[i - 1] = int.Parse(splited[0]);
                    HasTexturePos = true;
                    HasVertex = true;
                    break;
                case 3:
                    Normal[i - 1] = int.Parse(splited[2]);
                    TexPos[i - 1] = int.Parse(splited[1]);
                    Vertex[i - 1] = int.Parse(splited[0]);
                    HasNormal = true;
                    HasTexturePos = true;
                    HasVertex = true;
                    break;
            }
        }
    }
}

public sealed class TexturePosContent : BaseContent
{
    public Vector2 TexturePos;

    public TexturePosContent(string[] args)
    {
        TexturePos = new Vector2(
            float.Parse(args[1].Replace(".",",")),
            float.Parse(args[2].Replace(".",","))
        );
    }
}

public sealed class NormalContent : BaseContent
{
    public Vector3 Normal;

    public NormalContent(string[] args)
    {
        Normal = new Vector3(
            float.Parse(args[1].Replace(".",",")),
            float.Parse(args[2].Replace(".",",")),
            float.Parse(args[3].Replace(".",","))
        );
    }
}