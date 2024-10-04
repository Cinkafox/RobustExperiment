using System.IO;
using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class Mesh
{
    public List<Vector3> Vertexes = new();
    public List<Vector3> Normals = new();
    public List<FaceContent> Faces = new();
    public List<Vector2> TextureCoords = new();
    public List<Material> Materials = new();
    public Matrix4 Transform = Matrix4.Identity;

    public static Mesh Parse(TextReader textReader,ResPath path)
    {
        var mesh = new Mesh();
        var parser = new Objparser(textReader,path);
        
        var currMaterialId = -1;
        Dictionary<string, Material> materials = default!;

        foreach (var content in parser.Contents)
        {
            switch (content)
            {
                case VertexContent vertexContent:
                    mesh.Vertexes.Add(vertexContent.Vertex);
                    break;
                case FaceContent faceContent:
                    faceContent.MaterialId = currMaterialId;
                    mesh.Faces.Add(faceContent);
                    break;
                case TexturePosContent texturePosContent:
                    mesh.TextureCoords.Add(texturePosContent.TexturePos);
                    break;
                case NormalContent normalContent:
                    mesh.Normals.Add(normalContent.Normal);
                    break;
                case MaterialContent materialContent:
                    mesh.Materials.Add(materials[materialContent.Material]);
                    currMaterialId++;
                    break;
                case MtlLoadContent mtlLoadContent:
                    materials = mtlLoadContent.Materials;
                    break;
            }
        }
        
        return mesh;
    }
}