using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public static readonly int BuffSize = 2048 * 128;

    public readonly SimpleBuffer<Triangle> TriangleBuffer = new(BuffSize);
    public readonly Vector2[] uvBuff = new Vector2[BuffSize*3];

    public readonly SimpleBuffer<Vector4> InsidePoints = new(3);
    public readonly SimpleBuffer<Vector4> OutsidePoints = new(3);
    public readonly SimpleBuffer<Triangle> Clipping = new(2);
    
    public readonly SimpleBuffer<Texture> TextureBuffer = new(16);
    public readonly SimpleBuffer<int> TextureUsable = new(BuffSize);
    
    public readonly Vector2[] VectorBuffer = new Vector2[3];
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public readonly SimpleBuffer<Triangle> ClippingBuff = new(32);

    public void AppendTriangle(Triangle triangle, Vector2 p1, Vector2 p2, Vector2 p3, int textureId)
    {
        TriangleBuffer.Add(triangle);
        TextureUsable.Add(textureId);
        var leng = TriangleBuffer.Length - 1;
        
        uvBuff[leng*3] = p1;
        uvBuff[leng*3+1] = p2;
        uvBuff[leng*3+2] = p3;
    }

    public int AddTexture(Texture texture)
    {
        TextureBuffer.Add(texture);
        return TextureBuffer.Length - 1;
    }

    public void Flush()
    {
        TriangleBuffer.Clear();
        TextureUsable.Clear();
    }
}

public sealed class SimpleBuffer<T>
{
    private readonly T[] _buffer;
    
    public readonly int Limit;
    public int Length;
    public int Shift;
    public T this[int pos]
    {
        get => Get(pos);
        set => Set(pos,value);
    }

    public SimpleBuffer(int limit)
    {
        _buffer = new T[limit];
        Limit = limit;
    }

    public void Add(T obj)
    {
        _buffer[Shift + Length] = obj;
        Length++;
    }

    public T Pop()
    {
        var obj = _buffer[Shift + Length];

        Shift++;
        Length--;

        return obj;
    }

    public T Get(int pos)
    {
        return _buffer[Shift+pos];
    }

    public void Set(int pos, T obj)
    {
        _buffer[Shift+pos] = obj;
    }

    public void Clear()
    {
        Length = 0;
        Shift = 0;
    }

    public void Sort()
    {
        if(_buffer is not Triangle[] triangles) return;
        Sort(triangles,0,5);
    }
    
    public static void Sort(Triangle[] arr, int left, int right)
    {
        if (left < right)
        {
            var pivot = Partition(arr, left, right);
 
            Sort(arr, left, pivot - 1);
            Sort(arr, pivot + 1, right);
        }
    }
 
    private static int Partition(Triangle[] arr, int left, int right)
    {
        float pivot = arr[right].p1.Z;
        int i = left - 1;
 
        for (int j = left; j < right; j++)
        {
            if (arr[j].p1.Z <= pivot)
            {
                i++;
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
 
        (arr[i + 1], arr[right]) = (arr[right], arr[i + 1]);

        return i + 1;
    }
}

public sealed class DrawingHandle3d : IDisposable
{
    public bool Debug = false;
    public bool Disposed;

    private readonly DrawingHandleBase _handleBase;
    private readonly DrawingInstance _drawingInstance;

    public readonly float Width;
    public readonly float Height;

    private CameraProperties _cameraProperties;

    public Matrix4 ViewMatrix;
    public Matrix4 ProjectionMatrix;
    
    public void FlushScreenVec(Triangle triangle)
    {
        _drawingInstance.VectorBuffer[0] = ToScreenVec(triangle.p1.Xyz);
        _drawingInstance.VectorBuffer[1] = ToScreenVec(triangle.p2.Xyz);
        _drawingInstance.VectorBuffer[2] = ToScreenVec(triangle.p3.Xyz);
    }
    
    public Vector2 ToScreenVec(Vector3 vertex)
    {
        var vertex2D = new Vector2(vertex.X / vertex.Z, vertex.Y / vertex.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * Width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * Height;
        return new Vector2(screenX,screenY);
    }

    private void FlushScreen(Triangle triangle,Vector2 p1, Vector2 p2, Vector2 p3)
    {
        _drawingInstance.DrawVertexBuffer[0] = ToScreen(triangle.p1.Xyz,p1);
        _drawingInstance.DrawVertexBuffer[1] = ToScreen(triangle.p2.Xyz,p2);
        _drawingInstance.DrawVertexBuffer[2] = ToScreen(triangle.p3.Xyz,p3);
    }
    
    public DrawVertexUV2D ToScreen(Vector3 vertex, Vector2 uvPos)
    {
        return new DrawVertexUV2D(ToScreenVec(vertex),uvPos);
    }
    
    public void DrawPolygon(Triangle triangle, Vector2 p1, Vector2 p2, Vector2 p3, int TextureId)
    {
        CheckDisposed();
        triangle.Transform(ViewMatrix);
        
        var normal = triangle.Normal();
        normal.Normalize();

        var cameraToVertex = triangle.p1.Xyz;// - _cameraProperties.Position;

        var dotProduct = Vector3.Dot(normal, cameraToVertex);
        
        if(dotProduct >= 0) return;
        

        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.1f), new Vector3(0.0f, 0.0f, 1.0f), triangle,
            _drawingInstance);

        for (int i = 0; i < _drawingInstance.Clipping.Length; i++)
        {
            triangle.p1 = Vector3.Transform(_drawingInstance.Clipping[i].p1.Xyz, ProjectionMatrix).ToVec4();
            triangle.p2 = Vector3.Transform(_drawingInstance.Clipping[i].p2.Xyz, ProjectionMatrix).ToVec4();
            triangle.p3 = Vector3.Transform(_drawingInstance.Clipping[i].p3.Xyz, ProjectionMatrix).ToVec4();

            p1.X = p1.X / triangle.p1.W;
            p2.X = p2.X / triangle.p2.W;
            p3.X = p3.X / triangle.p3.W;
            p1.Y = p1.Y / triangle.p1.W;
            p2.Y = p2.Y / triangle.p2.W;
            p3.Y = p3.Y / triangle.p3.W;
            
            triangle.p1.X *= -1;
            triangle.p2.X *= -1;
            triangle.p3.X *= -1;
            triangle.p1.Y *= -1;
            triangle.p2.Y *= -1;
            triangle.p3.Y *= -1;
            
            _drawingInstance.AppendTriangle(triangle,p1,p2,p3,TextureId);
        }
    }

    public void Flush()
    {
        // for (int i = 0; i < _drawingInstance.TriangleBuffer.Length; i++)
        // {
        //     var triToRaster = _drawingInstance.TriangleBuffer[i];
        //     // Clip triangles against all four screen edges, this could yield
        //     // a bunch of triangles, so create a queue that we traverse to 
        //     //  ensure we only test new triangles generated against planes
        //     //List<Triangle> listTriangles = new List<Triangle>();
        //     _drawingInstance.ClippingBuff.Clear();
        //     
        //
        //     // Add initial triangle
        //     //listTriangles.Add(triToRaster);
        //     _drawingInstance.ClippingBuff[0] = triToRaster;
        //     int nNewTriangles = 1;
        //
        //     for (int p = 0; p < 4; p++)
        //     {
        //         while (nNewTriangles > 0)
        //         {
        //             // Take triangle from front of queue
        //             Triangle test = _drawingInstance.ClippingBuff.Pop();
        //             nNewTriangles--;
        //
        //             // Clip it against a plane. We only need to test each 
        //             // subsequent plane, against subsequent new triangles
        //             // as all triangles after a plane clip are guaranteed
        //             // to lie on the inside of the plane. I like how this
        //             // comment is almost completely and utterly justified
        //             switch (p)
        //             {
        //                 case 0:
        //                     Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),test,_drawingInstance);
        //                     break;
        //                 case 1:
        //                     Triangle.ClipAgainstClip(new Vector3(0.0f, Height - 1, 0.0f), new Vector3(0.0f, -1.0f, 0.0f),test,_drawingInstance);
        //                     break;
        //                 case 2:
        //                     Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f),test,_drawingInstance);
        //                     break;
        //                 case 3:
        //                     Triangle.ClipAgainstClip(new Vector3(Width - 1, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f),test,_drawingInstance);
        //                     break;
        //             }
        //
        //             // Clipping may yield a variable number of triangles, so
        //             // add these new ones to the back of the queue for subsequent
        //             // clipping against next planes
        //             for (int w = 0; w < _drawingInstance.Clipping.Length; w++)
        //                 _drawingInstance.ClippingBuff.Add(_drawingInstance.Clipping[w]);
        //         }
        //
        //         nNewTriangles = _drawingInstance.ClippingBuff.Length;
        //     }
        //
        //     for (int j = 0; j < _drawingInstance.ClippingBuff.Length; j++)
        //     {
        //         var triangle = _drawingInstance.ClippingBuff[j];
        //     
        //         if(Debug)
        //         {
        //             FlushScreenVec(triangle);
        //             _handleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop, _drawingInstance.VectorBuffer, Color.White);
        //         }
        //         else
        //         {
        //             FlushScreen(triangle,_drawingInstance.uvBuff[j*3],_drawingInstance.uvBuff[j*3+1],_drawingInstance.uvBuff[j*3+2]);
        //             _handleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList,_drawingInstance.Texture, _drawingInstance.DrawVertexBuffer);
        //         }
        //     }
        // }
        
        _drawingInstance.TriangleBuffer.Sort();
        
        for (int j = 0; j < _drawingInstance.TriangleBuffer.Length; j++)
        {
            var triangle = _drawingInstance.TriangleBuffer[j];
            var texture = _drawingInstance.TextureBuffer[_drawingInstance.TextureUsable[j]];
            
            if(Debug)
            {
                FlushScreenVec(triangle);
                _handleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop, _drawingInstance.VectorBuffer, Color.White);
            }
            else
            {
                FlushScreen(triangle,_drawingInstance.uvBuff[j*3],_drawingInstance.uvBuff[j*3+1],_drawingInstance.uvBuff[j*3+2]);
                _handleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture, _drawingInstance.DrawVertexBuffer);
            }
        }
        

        if (Debug)
        {
            var saPos = new Vector2(60, 60);
            _handleBase.DrawLine(saPos,saPos + new Vector2(_cameraProperties.CameraDirection.X,_cameraProperties.CameraDirection.Z)*saPos, Color.Aqua);
        }
        
        _drawingInstance.Flush();
        Dispose();
    }

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, CameraProperties cameraProperties,DrawingInstance drawingInstance)
    {
        _cameraProperties = cameraProperties;
        _drawingInstance = drawingInstance;
        _handleBase = handleBase;
        Width = width;
        Height = height;
            
        float cosPitch = float.Cos((float)_cameraProperties.Angle.Pitch);
        float sinPitch = float.Sin((float)_cameraProperties.Angle.Pitch);
        float cosYaw = float.Cos((float)_cameraProperties.Angle.Yaw);
        float sinYaw = float.Sin((float)_cameraProperties.Angle.Yaw);
        
        Vector3 xaxis = new Vector3(cosYaw, 0, -sinYaw);
        Vector3 yaxis = new Vector3(sinPitch * sinPitch, cosPitch, cosYaw * sinPitch);
        Vector3 zaxis = new Vector3(sinYaw * cosPitch, -sinPitch, cosPitch * cosYaw);
        
        ViewMatrix = new Matrix4(new Vector4(xaxis.X,            yaxis.X,            zaxis.X,      0), 
            new Vector4(xaxis.Y,            yaxis.Y,            zaxis.Y,      0),
            new Vector4(xaxis.Z,            yaxis.Z,            zaxis.Z,      0), 
            new Vector4(-Vector3.Dot(xaxis,_cameraProperties.Position),
                -Vector3.Dot(yaxis,_cameraProperties.Position),
                -Vector3.Dot(zaxis,_cameraProperties.Position), 1));

        
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            MathF.PI / _cameraProperties.FoV, // Field of view
            Width/Height, // Aspect ratio
            0.1f, // Near plane
            100.0f           // Far plane
        );
    }
    
    private void CheckDisposed()
    {
        if (Disposed)
        {
            throw new Exception("DISPODED");
        }
    }

    public void Dispose()
    {
        Disposed = true;
    }
}