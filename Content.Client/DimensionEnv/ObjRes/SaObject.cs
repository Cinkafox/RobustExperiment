﻿using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.Utils;
using Content.Client.Viewport;
using Robust.Client.Graphics;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

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

    private Vector4 v1;
    private Vector4 v2;
    private Vector4 v3;

    private Vector2 t1;
    private Vector2 t2;
    private Vector2 t3;

    private int matId;
    private Triangle triangle;

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
            foreach (var t in face.Vertex)
            {
                if (t >= 1 && t <= Mesh.Vertexes.Count) continue;
                Logger.Error($"Vertex index {t} is out of range!");
            }
            
            if (face.Vertex.Length == 3)
            {
                DrawPolygon(face, handle, 0, 1, 2);
            }
            else if (face.Vertex.Length > 3)
            {
                for (int i = 1; i < face.Vertex.Length - 1; i++)
                {
                    DrawPolygon(face, handle, 0, i, i + 1);
                }
            }
        }

        Mesh.Transform = null;
    }

    private void DrawPolygon(FaceContent face,DrawingHandle3d handle, int i1, int i2, int i3)
    {
        v1 = Mesh.Vertexes[face.Vertex[i1] - 1].ToVec4();
        v2 = Mesh.Vertexes[face.Vertex[i2] - 1].ToVec4();
        v3 = Mesh.Vertexes[face.Vertex[i3] - 1].ToVec4();

        triangle = new Triangle(v1, v2, v3);
        
        if (face.HasTexturePos)
        {
            matId = TextureBufferCoord + face.MaterialId;
            
            t1 = Mesh.TextureCoords[face.TexPos[i1] - 1];
            t2 = Mesh.TextureCoords[face.TexPos[i2] - 1];
            t3 = Mesh.TextureCoords[face.TexPos[i3] - 1];
        }
            
        handle.DrawPolygon(triangle, t1, t2, t3, matId);
    }
}