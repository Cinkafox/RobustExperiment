using System.Numerics;
using Content.Client.Utils;

namespace Content.Client.Viewport;

public sealed class ClippingInstance
{
    public readonly SimpleBuffer<Vector3> InsidePoints = new(3);
    public readonly SimpleBuffer<Vector3> OutsidePoints = new(3);
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
        
        var d0 = dist(inTri.Triangle.p1);
        var d1 = dist(inTri.Triangle.p2);
        var d2 = dist(inTri.Triangle.p3);
        
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
            var outTri1 = drawingInstance.AllocTriangle();
            
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
            var outTri1 = drawingInstance.AllocTriangle();
            var outTri2 = drawingInstance.AllocTriangle();
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
    
    public static Vector3 IntersectPlane(Vector3 plane_p, Vector3 plane_n, Vector3 lineStart, Vector3 lineEnd, out float t)
    {
        plane_n = Vector3.Normalize(plane_n);
        
        float plane_d = -Vector3.Dot(plane_n, plane_p);
        float ad = Vector3.Dot(lineStart, plane_n);
        float bd = Vector3.Dot(lineEnd, plane_n);
        t = (-plane_d - ad) / (bd - ad);
        var lineStartToEnd = Vector3.Subtract(lineEnd, lineStart);
        var lineToIntersect = Vector3.Multiply(lineStartToEnd, t);
        return Vector3.Add(lineStart, lineToIntersect);
    }
}