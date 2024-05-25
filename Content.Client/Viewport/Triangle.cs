using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Viewport;

public struct Triangle
{
    public Vector4 p1;
    public Vector4 p2;
    public Vector4 p3;

    public Triangle(Vector4 v1, Vector4 v2, Vector4 v3)
    {
        p1 = v1;
        p2 = v2;
        p3 = v3;
    }

    public void Transform(Matrix4 matrix4)
    {
        p1 = new Vector4(Vector3.Transform(p1.Xyz, matrix4),p1.W);
        p2 = new Vector4(Vector3.Transform(p2.Xyz, matrix4),p1.W);
        p3 = new Vector4(Vector3.Transform(p3.Xyz, matrix4),p1.W);
    }

    public Vector3 Normal()
    {
        var u = new Vector3(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        var v = new Vector3(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);

        float nx = u.Y * v.Z - u.Z * v.Y;
        float ny = u.Z * v.X - u.X * v.Z;
        float nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }

    public static void ClipAgainstClip(Vector3 planeP, Vector3 planeN,Triangle inTri,DrawingInstance drawingInstance)
    {
        planeN.Normalize();
        
        drawingInstance.OutsidePoints.Clear();
        drawingInstance.InsidePoints.Clear();
        drawingInstance.Clipping.Clear();

        float dist(Vector3 p)
        {
            var n = Vector3.Normalize(planeP);
            return (planeN.X * p.X + planeN.Y * p.Y + planeN.Z * p.Z - Vector3.Dot(planeN, planeP));
        }
        
        
        // Get signed distance of each point in triangle to plane
        float d0 = dist(inTri.p1.Xyz);
        float d1 = dist(inTri.p2.Xyz);
        float d2 = dist(inTri.p3.Xyz);
        
        if (d0 >= 0) { drawingInstance.InsidePoints.Add(inTri.p1); }
        else { drawingInstance.OutsidePoints.Add(inTri.p1); }
        if (d1 >= 0) { drawingInstance.InsidePoints.Add(inTri.p2); }
        else { drawingInstance.OutsidePoints.Add(inTri.p2); }
        if (d2 >= 0) { drawingInstance.InsidePoints.Add(inTri.p3); }
        else { drawingInstance.OutsidePoints.Add(inTri.p3); }
        
        if (drawingInstance.InsidePoints.Length == 3)
        {
            drawingInstance.Clipping[0] = inTri;
            drawingInstance.Clipping.Length = 1;
            return;
        }
        
        if (drawingInstance.InsidePoints.Length == 1 && drawingInstance.OutsidePoints.Length == 2)
        {
            var outTri1 = new Triangle
            {
                p1 = drawingInstance.InsidePoints[0],
                p2 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].Xyz, drawingInstance.OutsidePoints[0].Xyz).ToVec4(),
                p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].Xyz, drawingInstance.OutsidePoints[1].Xyz).ToVec4()
            };

            drawingInstance.Clipping[0] = outTri1;
            drawingInstance.Clipping.Length = 1;
            return;
        }

        if (drawingInstance.InsidePoints.Length == 2 && drawingInstance.OutsidePoints.Length == 1)
        {
            var outTri1 = new Triangle();
            var outTri2 = new Triangle();

            
            outTri1.p1 = drawingInstance.InsidePoints[0];
            outTri1.p2 = drawingInstance.InsidePoints[1];
            outTri1.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].Xyz, drawingInstance.OutsidePoints[0].Xyz).ToVec4();
            drawingInstance.Clipping[0] = outTri1;
            
            outTri2.p1 = drawingInstance.InsidePoints[1];
            outTri2.p2 = outTri1.p3;
            outTri2.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[1].Xyz, drawingInstance.OutsidePoints[0].Xyz).ToVec4();

            drawingInstance.Clipping[1] = outTri2;
            drawingInstance.Clipping.Length = 2;
        }
    }
}

public static class VectorExt
{
    public static Vector3 IntersectPlane(Vector3 plane_p, Vector3 plane_n, Vector3 lineStart, Vector3 lineEnd)
    {
        plane_n.Normalize();
        float plane_d = -Vector3.Dot(plane_n, plane_p);
        float ad = Vector3.Dot(lineStart, plane_n);
        float bd = Vector3.Dot(lineEnd, plane_n);
        float t = (-plane_d - ad) / (bd - ad);
        var lineStartToEnd = Vector3.Subtract(lineEnd, lineStart);
        var lineToIntersect = Vector3.Multiply(lineStartToEnd, t);
        return Vector3.Add(lineStart, lineToIntersect);
    }

    public static Vector4 ToVec4(this Vector3 vector3)
    {
        return new Vector4(vector3, 1);
    }
}
