using System.IO;
using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.DimensionEnv.ObjRes;

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

    public static Mesh Parse(TextReader textReader,ResPath path)
    {
        var mesh = new Mesh();
        var parser = new MeshParser(textReader,path);

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