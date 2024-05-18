using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public readonly Triangle[] TriangleBuffer = new Triangle[2048*128];
    public int CurrId;

    public void AppendTriangle(Triangle triangle)
    {
        TriangleBuffer[CurrId] = triangle;
        CurrId++;
    }

    public void Flush()
    {
        CurrId = 0;
    }
}

public sealed class DrawingHandle3d
{
    public bool Debug = true;
    
    public DrawingHandleBase HandleBase;

    public float Width;
    public float Height;
    
    public DrawingInstance DrawingInstance;
    
    public CameraProperties CameraProperties;
    
    public Matrix4 ViewMatrix;
    public Matrix4 ProjectionMatrix;

    public void Project(ref Triangle triangle)
    {
        triangle.Transform(ViewMatrix);
        triangle.Transform(ProjectionMatrix);
    }

    public Vector2[] ToScreenVec(Triangle triangle)
    {
        var dotArray = new Vector2[3];
        dotArray[0] = ToScreenVec(triangle.p1);
        dotArray[1] = ToScreenVec(triangle.p2);
        dotArray[2] = ToScreenVec(triangle.p3);

        return dotArray;
    }
    
    public Vector2 ToScreenVec(DrawVertexUV3D vertex)
    {
        var vertex2D = new Vector2(vertex.Position.X / vertex.Position.Z, vertex.Position.Y / vertex.Position.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * Width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * Height;
        return new Vector2(screenX,screenY);
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

        var cameraToVertex = triangle.p1.Position;

        var dotProduct = Vector3.Dot(normal, cameraToVertex);
        if(dotProduct >= 0) return;


        DrawingInstance.AppendTriangle(triangle);
    }

    public void Flush()
    {
        for (int i = 0; i < DrawingInstance.CurrId; i++)
        {
            var triangle = DrawingInstance.TriangleBuffer[i];
            
            if(Debug)
                HandleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop,ToScreenVec(triangle),Color.White);
            else
                HandleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList,triangle.Texture, ToScreen(triangle));
        }
        
        DrawingInstance.Flush();

        var saPos = new Vector2(60, 60);
        HandleBase.DrawLine(saPos,saPos + new Vector2(CameraProperties.CameraDirection.X,CameraProperties.CameraDirection.Z)*saPos, Color.Aqua);
    }

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, CameraProperties cameraProperties,DrawingInstance drawingInstance)
    {
        CameraProperties = cameraProperties;
        DrawingInstance = drawingInstance;
        HandleBase = handleBase;
        Width = width;
        Height = height;
        
        ViewMatrix = Matrix4.LookAt(CameraProperties.Position, CameraProperties.Position + CameraProperties.CameraDirection,Vector3.UnitY);
        
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            MathF.PI / CameraProperties.FoV, // Field of view
            Width/Height, // Aspect ratio
            0.1f, // Near plane
            100.0f           // Far plane
        );
    }
}