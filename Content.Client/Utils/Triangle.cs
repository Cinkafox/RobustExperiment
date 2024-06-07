using Content.Client.Viewport;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Utils;

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

    public void Clip(Triangle other)
    {
        
    }

    public static void ClipAgainstClip(Vector3 planeP, Vector3 planeN,TexturedTriangle inTri,DrawingInstance drawingInstance)
    {
        planeN.Normalize();
        
        drawingInstance.OutsidePoints.Clear();
        drawingInstance.InsidePoints.Clear();
        drawingInstance.InsideTex.Clear();
        drawingInstance.OutsideTex.Clear();
        drawingInstance.Clipping.Clear();

        float dist(Vector3 p)
        {
            var n = Vector3.Normalize(planeP);
            return (planeN.X * p.X + planeN.Y * p.Y + planeN.Z * p.Z - Vector3.Dot(planeN, planeP));
        }
        
        
        // Get signed distance of each point in triangle to plane
        float d0 = dist(inTri.Triangle.p1.Xyz);
        float d1 = dist(inTri.Triangle.p2.Xyz);
        float d2 = dist(inTri.Triangle.p3.Xyz);
        
        if (d0 >= 0) { drawingInstance.InsidePoints.Add(inTri.Triangle.p1); drawingInstance.InsideTex.Add(inTri.TexturePoint1);}
        else { drawingInstance.OutsidePoints.Add(inTri.Triangle.p1); drawingInstance.OutsideTex.Add(inTri.TexturePoint1);}
        if (d1 >= 0) { drawingInstance.InsidePoints.Add(inTri.Triangle.p2); drawingInstance.InsideTex.Add(inTri.TexturePoint2);}
        else { drawingInstance.OutsidePoints.Add(inTri.Triangle.p2); drawingInstance.OutsideTex.Add(inTri.TexturePoint2); }
        if (d2 >= 0) { drawingInstance.InsidePoints.Add(inTri.Triangle.p3); drawingInstance.InsideTex.Add(inTri.TexturePoint3);}
        else { drawingInstance.OutsidePoints.Add(inTri.Triangle.p3); drawingInstance.OutsideTex.Add(inTri.TexturePoint3);}
        
        if (drawingInstance.InsidePoints.Length == 3)
        {
            drawingInstance.Clipping[0] = inTri;
            drawingInstance.Clipping.Length = 1;
            return;
        }
        
        if (drawingInstance.InsidePoints.Length == 1 && drawingInstance.OutsidePoints.Length == 2)
        {
            var outTri1 = new TexturedTriangle();
            outTri1.Triangle.p1 = drawingInstance.InsidePoints[0];
            outTri1.Triangle.p2 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].Xyz,
                drawingInstance.OutsidePoints[0].Xyz).ToVec4();
            outTri1.Triangle.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].Xyz,
                drawingInstance.OutsidePoints[1].Xyz).ToVec4();

            outTri1.TextureId = inTri.TextureId;
            outTri1.TexturePoint1 = drawingInstance.InsideTex[0];
            outTri1.TexturePoint2 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];
            outTri1.TexturePoint3 = (drawingInstance.OutsideTex[1] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];

            drawingInstance.Clipping[0] = outTri1;
            drawingInstance.Clipping.Length = 1;
            return;
        }

        if (drawingInstance.InsidePoints.Length == 2 && drawingInstance.OutsidePoints.Length == 1)
        {
            var outTri1 = new TexturedTriangle();
            var outTri2 = new TexturedTriangle();
            
            outTri1.Triangle.p1 = drawingInstance.InsidePoints[0];
            outTri1.Triangle.p2 = drawingInstance.InsidePoints[1];
            outTri1.Triangle.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].Xyz, drawingInstance.OutsidePoints[0].Xyz).ToVec4();
            
            outTri1.TextureId = inTri.TextureId;
            outTri1.TexturePoint1 = drawingInstance.InsideTex[0];
            outTri1.TexturePoint2 = drawingInstance.InsideTex[1];
            outTri1.TexturePoint3 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];
            
            drawingInstance.Clipping[0] = outTri1;
            
            outTri2.Triangle.p1 = drawingInstance.InsidePoints[1];
            outTri2.Triangle.p2 = outTri1.Triangle.p3;
            outTri2.Triangle.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[1].Xyz, drawingInstance.OutsidePoints[0].Xyz).ToVec4();
            
            outTri2.TextureId = inTri.TextureId;
            outTri2.TexturePoint1 = drawingInstance.InsideTex[1];
            outTri2.TexturePoint2 = outTri1.TexturePoint3;
            outTri2.TexturePoint3 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[1]) + drawingInstance.InsideTex[1];
            
            drawingInstance.Clipping[1] = outTri2;
            drawingInstance.Clipping.Length = 2;
        }
    }
}