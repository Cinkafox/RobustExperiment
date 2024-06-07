using Content.Client.Utils;
using Content.Client.Viewport;
using Robust.Client;
using Robust.Client.Graphics;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class SaObject
{
    public Mesh Mesh;
    public int TextureBufferCoord { get; private set; }

    public SaObject(Mesh mesh, int textureBufferCoord)
    {
        Mesh = mesh;
        TextureBufferCoord = textureBufferCoord;
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
            var appended = face.Vertex.Length >= 4;
            var matId = TextureBufferCoord + face.MaterialId;

            var v1 = Mesh.Vertexes[face.Vertex[0] - 1].ToVec4();
            var v2 = Mesh.Vertexes[face.Vertex[1] - 1].ToVec4();
            var v3 = Mesh.Vertexes[face.Vertex[2] - 1].ToVec4();

            var triangle = new Triangle(v1, v2, v3);

            if (face.HasTexturePos)
            {
                var t1 = Mesh.TextureCoords[face.TexPos[0] - 1];
                var t2 = Mesh.TextureCoords[face.TexPos[1] - 1];
                var t3 = Mesh.TextureCoords[face.TexPos[2] - 1];

                handle.DrawPolygon(triangle, t1, t2, t3, matId);
            }

            if (appended)
            {
                var va1 = Mesh.Vertexes[face.Vertex[0] - 1].ToVec4();
                var va2 = Mesh.Vertexes[face.Vertex[2] - 1].ToVec4();
                var va3 = Mesh.Vertexes[face.Vertex[3] - 1].ToVec4();

                var triangle1 = new Triangle(va1, va2, va3);

                if (face.HasTexturePos)
                {
                    var t1 = Mesh.TextureCoords[face.TexPos[0] - 1];
                    var t2 = Mesh.TextureCoords[face.TexPos[2] - 1];
                    var t3 = Mesh.TextureCoords[face.TexPos[3] - 1];

                    handle.DrawPolygon(triangle1, t1, t2, t3, matId);
                }
            }
        }

        Mesh.Transform = null;
    }
}