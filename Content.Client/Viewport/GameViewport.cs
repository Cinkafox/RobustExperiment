using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Viewport;

public sealed class GameViewport : Control
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private Texture _texture;

    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        _texture = _resourceCache.GetResource<TextureResource>("/Textures/fat-gorilla.png").Texture;
    }

    public Matrix4? CurrentTransform = Matrix4.CreateRotationX(0.003f) * Matrix4.CreateRotationY(0.001f) * Matrix4.CreateRotationZ(0.002f);

    public DrawVertexUV3D[] Vertexes = new DrawVertexUV3D[]
    {
        new DrawVertexUV3D(new Vector3(0,0,0), new Vector2(0,0)),
        new DrawVertexUV3D(new Vector3(0,1,0), new Vector2(1,0)),
        new DrawVertexUV3D(new Vector3(1,0,0), new Vector2(0,1)),
        new DrawVertexUV3D(new Vector3(1,1,0), new Vector2(1,1)),
        
        new DrawVertexUV3D(new Vector3(1,0,0), new Vector2(0,0)),
        new DrawVertexUV3D(new Vector3(1,1,0), new Vector2(1,0)),
        new DrawVertexUV3D(new Vector3(1,0,1), new Vector2(0,1)),
        new DrawVertexUV3D(new Vector3(1,1,1), new Vector2(1,1)),
        
        new DrawVertexUV3D(new Vector3(1,0,1), new Vector2(0,0)),
        new DrawVertexUV3D(new Vector3(1,1,1), new Vector2(1,0)),
        new DrawVertexUV3D(new Vector3(0,0,1), new Vector2(0,1)),
        new DrawVertexUV3D(new Vector3(0,1,1), new Vector2(1,1)),
        
        new DrawVertexUV3D(new Vector3(0,0,1), new Vector2(0,0)),
        new DrawVertexUV3D(new Vector3(0,1,1), new Vector2(1,0)),
        new DrawVertexUV3D(new Vector3(0,0,0), new Vector2(0,1)),
        new DrawVertexUV3D(new Vector3(0,1,0), new Vector2(1,1)),
        
        new DrawVertexUV3D(new Vector3(0,1,0), new Vector2(0,0)),
        new DrawVertexUV3D(new Vector3(0,1,1), new Vector2(1,0)),
        new DrawVertexUV3D(new Vector3(1,1,0), new Vector2(0,1)),
        new DrawVertexUV3D(new Vector3(1,1,1), new Vector2(1,1)),
        
        new DrawVertexUV3D(new Vector3(1,0,1), new Vector2(0,0)),
        new DrawVertexUV3D(new Vector3(0,0,1), new Vector2(1,0)),
        new DrawVertexUV3D(new Vector3(1,0,0), new Vector2(0,1)),
        new DrawVertexUV3D(new Vector3(0,0,0), new Vector2(1,1)),
        
    };

    private int ticked = 0;

    protected override void Draw(DrawingHandleScreen handle)
    {
        ticked = (ticked + 1) % 32;
        if (ticked == 0)
        {
            
        }
        
        
        var drawHandle = new DrawingHandle3d(handle,Width,Height);

        if(CurrentTransform != null)
        {
            for (int i = Vertexes.Length - 1; i >= 0; i--)
            {
                Vertexes[i].Position = Vector3.Transform(Vertexes[i].Position, CurrentTransform.Value);
            }
            //CurrentTransform = null;
        }
        
        
        drawHandle.DrawPolygon(new Triangle(Vertexes[0], Vertexes[3], Vertexes[1], _texture));
        drawHandle.DrawPolygon(new Triangle(Vertexes[0], Vertexes[2], Vertexes[3], _texture));
        
        drawHandle.DrawPolygon(new Triangle(Vertexes[4], Vertexes[7], Vertexes[5], _texture));
        drawHandle.DrawPolygon(new Triangle(Vertexes[4], Vertexes[6], Vertexes[7], _texture));
         
        drawHandle.DrawPolygon(new Triangle(Vertexes[8], Vertexes[11], Vertexes[9], _texture));
        drawHandle.DrawPolygon(new Triangle(Vertexes[8], Vertexes[10], Vertexes[11], _texture));
         
        drawHandle.DrawPolygon(new Triangle(Vertexes[12], Vertexes[15], Vertexes[13], _texture));
        drawHandle.DrawPolygon(new Triangle(Vertexes[12], Vertexes[14], Vertexes[15], _texture));
         
        drawHandle.DrawPolygon(new Triangle(Vertexes[16], Vertexes[19], Vertexes[17], _texture));
        drawHandle.DrawPolygon(new Triangle(Vertexes[16], Vertexes[18], Vertexes[19], _texture));
        
        drawHandle.DrawPolygon(new Triangle(Vertexes[20], Vertexes[23], Vertexes[21], _texture));
        drawHandle.DrawPolygon(new Triangle(Vertexes[20], Vertexes[22], Vertexes[23], _texture));
        
        drawHandle.Draw();
    }
}

public sealed class DrawingHandle3d
{
    public DrawingHandleBase HandleBase;

    public float Width;
    public float Height;
    
    public List<Triangle> ProjectedMesh = new();

    public Vector3 CameraPos = new Vector3(0, 0, 3);
    
    public Matrix4 ViewMatrix;
    public Matrix4 ProjectionMatrix;

    public void Project(ref Triangle triangle)
    {
        triangle.Transform(ViewMatrix);
        triangle.Transform(ProjectionMatrix);
    }

    public DrawVertexUV2D[] ToScreen(Triangle triangle)
    {
        var dotArray = new DrawVertexUV2D[3];
        dotArray[0] = ToScreen(triangle.p1);
        dotArray[1] = ToScreen(triangle.p2);
        dotArray[2] = ToScreen(triangle.p3);

        return dotArray;
    }

    public DrawVertexUV2D ToScreen(DrawVertexUV3D vertex)
    {
        var vertex2D = new Vector2(vertex.Position.X / vertex.Position.Z, vertex.Position.Y / vertex.Position.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * Width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * Height;
        return new DrawVertexUV2D(new Vector2(screenX,screenY), vertex.UV);
    }

    public void DrawPolygon(Triangle triangle)
    {
        Project(ref triangle);

        var normal = triangle.Normal();
        
        normal.Normalize();
        

        var cameraToVertex = triangle.p1.Position ;

        var dotProduct = Vector3.Dot(normal, cameraToVertex);
        //Logger.Debug(dotProduct + " " + normal + " " + cameraToVertex);
        if(dotProduct >= 0) return;
            
        ProjectedMesh.Add(triangle);
    }

    public void Draw()
    {
        foreach (var triangle in ProjectedMesh)
        {
            //HandleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop,ToScreen(triangle).Select(a => a.Position).ToArray(),Color.White);
            
            //continue;
            HandleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList,triangle.Texture, ToScreen(triangle));
        }
    }

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height)
    {
        HandleBase = handleBase;
        Width = width;
        Height = height;

        ViewMatrix = Matrix4.LookAt(CameraPos, new Vector3(0.5f,0.5f,0.5f),Vector3.UnitY);
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            MathF.PI / 4.0f, // Field of view
            Width/Height, // Aspect ratio
            0.1f, // Near plane
            100.0f           // Far plane)
        );
    }
}

public struct Triangle
{
    public DrawVertexUV3D p1;
    public DrawVertexUV3D p2;
    public DrawVertexUV3D p3;
    public Texture Texture;

    public Triangle(DrawVertexUV3D p1, DrawVertexUV3D p2, DrawVertexUV3D p3, Texture texture)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
        Texture = texture;
    }

    public void Transform(Matrix4 matrix4)
    {
        p1.Transform(matrix4);
        p2.Transform(matrix4);
        p3.Transform(matrix4);
    }

    public Vector3 Normal()
    {
        var u = new Vector3(p2.Position.X - p1.Position.X, p2.Position.Y - p1.Position.Y, p2.Position.Z - p1.Position.Z);
        var v = new Vector3(p3.Position.X - p1.Position.X, p3.Position.Y - p1.Position.Y, p3.Position.Z - p1.Position.Z);

        float nx = u.Y * v.Z - u.Z * v.Y;
        float ny = u.Z * v.X - u.X * v.Z;
        float nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }
}

public struct DrawVertexUV3D
{
    public Vector3 Position;
    public Vector2 UV;

    public DrawVertexUV3D(Vector3 position, Vector2 uv)
    {
        Position = position;
        UV = uv;
    }

    public void Transform(Matrix4 matrix4)
    {
        Position = Vector3.Transform(Position, matrix4);
    }
}