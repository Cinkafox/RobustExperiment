using System.Numerics;
using Content.Client.Utils;

namespace Content.Client.Viewport;

public sealed class ClippingInstance
{
    public readonly SimpleBuffer<Vector4> InsidePoints = new(3);
    public readonly SimpleBuffer<Vector4> OutsidePoints = new(3);
    public readonly SimpleBuffer<Vector2> InsideTex = new(3);
    public readonly SimpleBuffer<Vector2> OutsideTex = new(3);
    public readonly SimpleBuffer<TexturedTriangle> Clipping = new(2);

    public void Clear()
    {
        InsidePoints.Clear();
        OutsidePoints.Clear();
        InsideTex.Clear();
        OutsideTex.Clear();
        Clipping.Clear();
    }
    
    public void ClipAgainstClip(Vector3 planeP, Vector3 planeN, TexturedTriangle inTri, DrawingInstance drawingInstance)
    {
        planeN = Vector3.Normalize(planeN);
        
        Clear();

        float dist(Vector3 p)
        {
            return Vector3.Dot(planeN, p - planeP);
        }
        
        var d0 = dist(inTri.Triangle.GetP1());
        var d1 = dist(inTri.Triangle.GetP2());
        var d2 = dist(inTri.Triangle.GetP3());
        
        if (d0 >= 0) { InsidePoints.Add(inTri.Triangle.p1); InsideTex.Add(inTri.TexturePoint1);}
        else { OutsidePoints.Add(inTri.Triangle.p1); OutsideTex.Add(inTri.TexturePoint1);}
        if (d1 >= 0) { InsidePoints.Add(inTri.Triangle.p2); InsideTex.Add(inTri.TexturePoint2);}
        else { OutsidePoints.Add(inTri.Triangle.p2); OutsideTex.Add(inTri.TexturePoint2); }
        if (d2 >= 0) { InsidePoints.Add(inTri.Triangle.p3); InsideTex.Add(inTri.TexturePoint3);}
        else { OutsidePoints.Add(inTri.Triangle.p3); OutsideTex.Add(inTri.TexturePoint3);}
        
        if (InsidePoints.Length == 3)
        {
            Clipping.Add(inTri);
            return;
        }
        
        if (InsidePoints.Length == 1 && OutsidePoints.Length == 2)
        {
            var outTri1 = drawingInstance.TriangleBuffer.Take();
            
            outTri1.TextureId = inTri.TextureId;
            
            outTri1.Triangle.p1 = InsidePoints[0];
            outTri1.TexturePoint1 = InsideTex[0];
            
            outTri1.Triangle.p2 = IntersectPlane(planeP, planeN, InsidePoints[0],
                OutsidePoints[0], out var t);
            outTri1.TexturePoint2 = t * (OutsideTex[0] - InsideTex[0]) + InsideTex[0];
            
            outTri1.Triangle.p3 = IntersectPlane(planeP, planeN, InsidePoints[0],
                OutsidePoints[1], out t);
            outTri1.TexturePoint3 = t * (OutsideTex[1] - InsideTex[0]) + InsideTex[0];
            
            Clipping.Add(outTri1);
            return;
        }

        if (InsidePoints.Length == 2 && OutsidePoints.Length == 1)
        {
            var outTri1 = drawingInstance.TriangleBuffer.Take();
            var outTri2 = drawingInstance.TriangleBuffer.Take();
            outTri1.TextureId = inTri.TextureId;
            outTri2.TextureId = inTri.TextureId;
            
            outTri1.Triangle.p1 = InsidePoints[0];
            outTri1.Triangle.p2 = InsidePoints[1];
            outTri1.Triangle.p3 = IntersectPlane(planeP, planeN, InsidePoints[0], OutsidePoints[0], out var t);
            
            outTri1.TexturePoint1 = InsideTex[0];
            outTri1.TexturePoint2 = InsideTex[1];
            outTri1.TexturePoint3 = t * (OutsideTex[0] - InsideTex[0]) + InsideTex[0];
            
            outTri2.Triangle.p1 = InsidePoints[1];
            outTri2.Triangle.p2 = outTri1.Triangle.p3;
            outTri2.Triangle.p3 = IntersectPlane(planeP, planeN, InsidePoints[1], OutsidePoints[0], out t);
            
            outTri2.TexturePoint1 = InsideTex[1];
            outTri2.TexturePoint2 = outTri1.TexturePoint3;
            outTri2.TexturePoint3 = t * (OutsideTex[0] - InsideTex[1]) + InsideTex[1];
            
            Clipping.Add(outTri1);
            Clipping.Add(outTri2);
        }
    }
    
    public static Vector4 IntersectPlane(Vector3 plane_p, Vector3 plane_n, Vector4 lineStart, Vector4 lineEnd, out float t)
    {
        plane_n = Vector3.Normalize(plane_n);
        var plane_d = -Vector3.Dot(plane_n, plane_p);
        
        var p0 = (Math.Abs(lineStart.W) > 1e-6f) 
            ? new Vector3(lineStart.X / lineStart.W, lineStart.Y / lineStart.W, lineStart.Z / lineStart.W) 
            : new Vector3(lineStart.X, lineStart.Y, lineStart.Z);
        
        var p1 = (Math.Abs(lineEnd.W) > 1e-6f) 
            ? new Vector3(lineEnd.X / lineEnd.W, lineEnd.Y / lineEnd.W, lineEnd.Z / lineEnd.W) 
            : new Vector3(lineEnd.X, lineEnd.Y, lineEnd.Z);
        
        var dist0 = Vector3.Dot(p0, plane_n) + plane_d;
        var dist1 = Vector3.Dot(p1, plane_n) + plane_d;
        
        var denom = dist1 - dist0;
        if (Math.Abs(denom) < 1e-6f)
        {
            t = 0.0f;
            return Vector4.Zero; 
        }
        
        t = -dist0 / denom;
        
        var intersectCartesian = p0 + (p1 - p0) * t;

        float wResult;
        if (Math.Abs(lineStart.W) > 1e-6f && Math.Abs(lineEnd.W) > 1e-6f)
        {
            var invW0 = 1.0f / lineStart.W;
            var invW1 = 1.0f / lineEnd.W;
            var invWResult = (1.0f - t) * invW0 + t * invW1;
            wResult = (Math.Abs(invWResult) > 1e-6f) ? 1.0f / invWResult : 1.0f;
        }
        else
        {
            wResult = 1.0f;
        }
        
        return new Vector4(
            intersectCartesian.X * wResult,
            intersectCartesian.Y * wResult,
            intersectCartesian.Z * wResult,
            wResult
        );
    }
}