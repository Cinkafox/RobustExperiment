using System.Numerics;
using Content.Client.DimensionEnv.ObjRes;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Robust.Client.Graphics;
using Robust.Client.Utility;

namespace Content.Client.SkyBoxes;

public sealed class SkyBoxSystem: EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SkyBoxComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<SkyBoxComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Texture = ent.Comp.SkyboxPath.Frame0();
    }

    public Mesh CreateBox(Texture text)
    {
        var baseSize = 5f;
        return new Mesh()
        {
            Vertexes =
            {
                new Vector3(-baseSize, -baseSize, -baseSize), new Vector3(-baseSize, -baseSize, baseSize), 
                new Vector3(baseSize, -baseSize, -baseSize), new Vector3(baseSize, -baseSize, baseSize),
                
                new Vector3(-baseSize, baseSize, -baseSize), new Vector3(-baseSize, baseSize, baseSize), 
                new Vector3(baseSize, baseSize, -baseSize), new Vector3(baseSize, baseSize, baseSize),
            },
            TextureCoords =
            {
                new Vector2(1/4f,0),new Vector2(1/4f,1/3f),new Vector2(2/4f,0f),new Vector2(2/4f,1/3f), //UP
                new Vector2(1/4f,2/3f),new Vector2(1/4f,3/3f),new Vector2(2/4f,2/3f),new Vector2(2/4f,3/3f), //BOTTOM
                new Vector2(0,1/3f),new Vector2(0,2/3f),new Vector2(1/4f,1/3f),new Vector2(1/4f,2/3f), //BACK
            },
            Faces =
            {
                //UP
                new Face([
                    new FaceVertex(1,1),new FaceVertex(2,2),new FaceVertex(4,4)
                ])
                {
                    MaterialId = 0
                },
                new Face([
                    new FaceVertex(3,3),new FaceVertex(1,1),new FaceVertex(4,4)
                ])
                {
                    MaterialId = 0
                },
                
                //BOTTOM
                new Face([
                    new FaceVertex(5,5),new FaceVertex(7,7),new FaceVertex(8,8)
                ])
                {
                    MaterialId = 0
                },
                new Face([
                    new FaceVertex(6,6),new FaceVertex(5,5),new FaceVertex(8,8)
                ])
                {
                    MaterialId = 0
                },
                
                //BACK
                new Face([
                    new FaceVertex(1,9),new FaceVertex(3,10),new FaceVertex(7,12)
                ])
                {
                    MaterialId = 0
                },
                new Face([
                    new FaceVertex(5,11),new FaceVertex(1,9),new FaceVertex(7,12)
                ])
                {
                    MaterialId = 0
                },
                
                //FRONT
                new Face([
                    new FaceVertex(4,7),new FaceVertex(2,5),new FaceVertex(8,8)
                ])
                {
                    MaterialId = 0
                },
                new Face([
                    new FaceVertex(2,6),new FaceVertex(6,5),new FaceVertex(8,8)
                ])
                {
                    MaterialId = 0
                },
                
                //RIGHT
                new Face([
                    new FaceVertex(1,7),new FaceVertex(5,5),new FaceVertex(6,8)
                ])
                {
                    MaterialId = 0
                },
                new Face([
                    new FaceVertex(2,5),new FaceVertex(1,6),new FaceVertex(6,8)
                ])
                {
                    MaterialId = 0
                },
                
                //LEFT
                new Face([
                    new FaceVertex(3,7),new FaceVertex(4,5),new FaceVertex(8,8)
                ])
                {
                    MaterialId = 0
                },
                new Face([
                    new FaceVertex(7,5),new FaceVertex(3,6),new FaceVertex(8,8)
                ])
                {
                    MaterialId = 0
                },
                
            },
            Materials =
            {
                new Material(Vector3.Zero,Vector3.Zero,Vector3.Zero,0,Vector3.Zero,0,0,0,null, text)
            }
        };
    }
}