using System.Collections;
using System.Numerics;
using Content.Client.Viewport;

namespace Content.Client.Utils;

public struct Triangle : IEnumerable<Vector4>
{
    public Vector4 p1;
    public Vector4 p2;
    public Vector4 p3;

    public float Z => (p1.Z + p2.Z + p3.Z) / 3;

    public Triangle(Vector4 v1, Vector4 v2, Vector4 v3)
    {
        p1 = v1;
        p2 = v2;
        p3 = v3;
    }

    public void Transform(Matrix4x4 matrix4)
    {
        p1 = new Vector4(Vector3.Transform(p1.ToVec3(), matrix4),p1.W);
        p2 = new Vector4(Vector3.Transform(p2.ToVec3(), matrix4),p1.W);
        p3 = new Vector4(Vector3.Transform(p3.ToVec3(), matrix4),p1.W);
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
    
    public static bool IsPointInsideTriangle(Vector3 point, Triangle triangle)
    {
        var v0 = triangle.p3.ToVec3() - triangle.p1.ToVec3();
        var v1 = triangle.p2.ToVec3() - triangle.p1.ToVec3();
        var v2 = point - triangle.p1.ToVec3();

        var dot00 = Vector3.Dot(v0, v0);
        var dot01 = Vector3.Dot(v0, v1);
        var dot02 = Vector3.Dot(v0, v2);
        var dot11 = Vector3.Dot(v1, v1);
        var dot12 = Vector3.Dot(v1, v2);

        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
        
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }
    
    public static bool IsPointInsideTriangle(Vector2 p, Triangle triangle)
    {
        var p1 = triangle.p1;
        var p2 = triangle.p2;
        var p3 = triangle.p3;
        
        float areaOrig = Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y));
        float area1 = Math.Abs((p.X - p1.X) * (p2.Y - p1.Y) - (p2.X - p1.X) * (p.Y - p1.Y));
        float area2 = Math.Abs((p.X - p2.X) * (p3.Y - p2.Y) - (p3.X - p2.X) * (p.Y - p2.Y));
        float area3 = Math.Abs((p.X - p3.X) * (p1.Y - p3.Y) - (p1.X - p3.X) * (p.Y - p3.Y));

        return (areaOrig == area1 + area2 + area3);
    }
    
    public static void ClipAgainstTriangle(TexturedTriangle inTri, TexturedTriangle otherTri, DrawingInstance drawingInstance)
    {
        drawingInstance.OutsidePoints.Clear();
        drawingInstance.InsidePoints.Clear();
        drawingInstance.InsideTex.Clear();
        drawingInstance.OutsideTex.Clear();
        drawingInstance.Clipping.Clear();

        // Проверяем каждую вершину inTri на нахождение внутри otherTri
        if (IsPointInsideTriangle(inTri.Triangle.p1.ToVec3(), otherTri.Triangle))
        {
            drawingInstance.InsidePoints.Add(inTri.Triangle.p1);
            drawingInstance.InsideTex.Add(inTri.TexturePoint1);
        }
        else
        {
            drawingInstance.OutsidePoints.Add(inTri.Triangle.p1);
            drawingInstance.OutsideTex.Add(inTri.TexturePoint1);
        }

        if (IsPointInsideTriangle(inTri.Triangle.p2.ToVec3(), otherTri.Triangle))
        {
            drawingInstance.InsidePoints.Add(inTri.Triangle.p2);
            drawingInstance.InsideTex.Add(inTri.TexturePoint2);
        }
        else
        {
            drawingInstance.OutsidePoints.Add(inTri.Triangle.p2);
            drawingInstance.OutsideTex.Add(inTri.TexturePoint2);
        }

        if (IsPointInsideTriangle(inTri.Triangle.p3.ToVec3(), otherTri.Triangle))
        {
            drawingInstance.InsidePoints.Add(inTri.Triangle.p3);
            drawingInstance.InsideTex.Add(inTri.TexturePoint3);
        }
        else
        {
            drawingInstance.OutsidePoints.Add(inTri.Triangle.p3);
            drawingInstance.OutsideTex.Add(inTri.TexturePoint3);
        }

        // Если все точки внутри, то добавляем треугольник без изменений
        if (drawingInstance.InsidePoints.Length == 3)
        {
            drawingInstance.Clipping[0] = inTri;
            drawingInstance.Clipping.Length = 1;
            return;
        }

        // Если одна точка внутри, две снаружи — пересекаем грани треугольников
        if (drawingInstance.InsidePoints.Length == 1 && drawingInstance.OutsidePoints.Length == 2)
        {
            var outTri1 = new TexturedTriangle();
            outTri1.Triangle.p1 = drawingInstance.InsidePoints[0];
            outTri1.Triangle.p2 = VectorExtTriangle.IntersectSegmentWithTriangle(drawingInstance.InsidePoints[0].ToVec3(), drawingInstance.OutsidePoints[0].ToVec3(), otherTri).ToVec4();
            outTri1.Triangle.p3 = VectorExtTriangle.IntersectSegmentWithTriangle(drawingInstance.InsidePoints[0].ToVec3(), drawingInstance.OutsidePoints[1].ToVec3(), otherTri).ToVec4();

            outTri1.TextureId = inTri.TextureId;
            outTri1.TexturePoint1 = drawingInstance.InsideTex[0];
            outTri1.TexturePoint2 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];
            outTri1.TexturePoint3 = (drawingInstance.OutsideTex[1] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];

            drawingInstance.Clipping[0] = outTri1;
            drawingInstance.Clipping.Length = 1;
            return;
        }

        // Если две точки внутри, одна снаружи — пересекаем грани треугольников
        if (drawingInstance.InsidePoints.Length == 2 && drawingInstance.OutsidePoints.Length == 1)
        {
            var outTri1 = new TexturedTriangle();
            var outTri2 = new TexturedTriangle();

            outTri1.Triangle.p1 = drawingInstance.InsidePoints[0];
            outTri1.Triangle.p2 = drawingInstance.InsidePoints[1];
            outTri1.Triangle.p3 = VectorExtTriangle.IntersectSegmentWithTriangle(drawingInstance.InsidePoints[0].ToVec3(), drawingInstance.OutsidePoints[0].ToVec3(), otherTri).ToVec4();

            outTri1.TextureId = inTri.TextureId;
            outTri1.TexturePoint1 = drawingInstance.InsideTex[0];
            outTri1.TexturePoint2 = drawingInstance.InsideTex[1];
            outTri1.TexturePoint3 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];

            drawingInstance.Clipping[0] = outTri1;

            outTri2.Triangle.p1 = drawingInstance.InsidePoints[1];
            outTri2.Triangle.p2 = outTri1.Triangle.p3;
            outTri2.Triangle.p3 = VectorExtTriangle.IntersectSegmentWithTriangle(drawingInstance.InsidePoints[1].ToVec3(), drawingInstance.OutsidePoints[0].ToVec3(), otherTri).ToVec4();

            outTri2.TextureId = inTri.TextureId;
            outTri2.TexturePoint1 = drawingInstance.InsideTex[1];
            outTri2.TexturePoint2 = outTri1.TexturePoint3;
            outTri2.TexturePoint3 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[1]) + drawingInstance.InsideTex[1];

            drawingInstance.Clipping[1] = outTri2;
            drawingInstance.Clipping.Length = 2;
        }
    }

    public static void ClipAgainstClip(Vector3 planeP, Vector3 planeN,TexturedTriangle inTri,DrawingInstance drawingInstance)
    {
        planeN = Vector3.Normalize(planeN);
        
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
        float d0 = dist(inTri.Triangle.p1.ToVec3());
        float d1 = dist(inTri.Triangle.p2.ToVec3());
        float d2 = dist(inTri.Triangle.p3.ToVec3());
        
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
            outTri1.Triangle.p2 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].ToVec3(),
                drawingInstance.OutsidePoints[0].ToVec3()).ToVec4();
            outTri1.Triangle.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].ToVec3(),
                drawingInstance.OutsidePoints[1].ToVec3()).ToVec4();

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
            outTri1.Triangle.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[0].ToVec3(), drawingInstance.OutsidePoints[0].ToVec3()).ToVec4();
            
            outTri1.TextureId = inTri.TextureId;
            outTri1.TexturePoint1 = drawingInstance.InsideTex[0];
            outTri1.TexturePoint2 = drawingInstance.InsideTex[1];
            outTri1.TexturePoint3 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[0]) + drawingInstance.InsideTex[0];
            
            drawingInstance.Clipping[0] = outTri1;
            
            outTri2.Triangle.p1 = drawingInstance.InsidePoints[1];
            outTri2.Triangle.p2 = outTri1.Triangle.p3;
            outTri2.Triangle.p3 = VectorExt.IntersectPlane(planeP, planeN, drawingInstance.InsidePoints[1].ToVec3(), drawingInstance.OutsidePoints[0].ToVec3()).ToVec4();
            
            outTri2.TextureId = inTri.TextureId;
            outTri2.TexturePoint1 = drawingInstance.InsideTex[1];
            outTri2.TexturePoint2 = outTri1.TexturePoint3;
            outTri2.TexturePoint3 = (drawingInstance.OutsideTex[0] - drawingInstance.InsideTex[1]) + drawingInstance.InsideTex[1];
            
            drawingInstance.Clipping[1] = outTri2;
            drawingInstance.Clipping.Length = 2;
        }
    }

    public IEnumerator<Vector4> GetEnumerator()
    {
        yield return p1;
        yield return p2;
        yield return p3;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class VectorExtTriangle
{
    // Метод для нахождения точки пересечения отрезка (start, end) с плоскостью треугольника
    public static Vector3 IntersectSegmentWithTriangle(Vector3 segmentStart, Vector3 segmentEnd, TexturedTriangle tri)
    {
        // 1. Вычисляем нормаль плоскости треугольника
        Vector3 v0 = tri.Triangle.p2.ToVec3() - tri.Triangle.p1.ToVec3();
        Vector3 v1 = tri.Triangle.p3.ToVec3() - tri.Triangle.p1.ToVec3();
        Vector3 normal = Vector3.Cross(v0, v1);
        normal = Vector3.Normalize(normal);

        // 2. Вычисляем плоскость: нормаль и точка на плоскости (p1)
        Vector3 planePoint = tri.Triangle.p1.ToVec3();

        // 3. Находим точку пересечения отрезка с плоскостью
        Vector3 segmentDirection = segmentEnd - segmentStart;
        float dotProduct = Vector3.Dot(normal, segmentDirection);

        // Проверяем, если отрезок параллелен плоскости (нет пересечения)
        if (Math.Abs(dotProduct) < 1e-6f)
        {
            // Отрезок и плоскость не пересекаются или отрезок лежит в плоскости
            return Vector3.Zero; // Возвращаем нулевой вектор как индикатор отсутствия пересечения
        }

        // 4. Вычисляем точку пересечения с плоскостью
        float t = Vector3.Dot(normal, (planePoint - segmentStart)) / dotProduct;
        
        // Проверяем, если пересечение находится за пределами отрезка
        if (t < 0.0f || t > 1.0f)
        {
            return Vector3.Zero; // Точка пересечения не на отрезке
        }

        // 5. Получаем точку пересечения
        Vector3 intersectionPoint = segmentStart + t * segmentDirection;

        // 6. Проверяем, лежит ли точка внутри треугольника
        if (Triangle.IsPointInsideTriangle(intersectionPoint, tri.Triangle))
        {
            return intersectionPoint;
        }

        return Vector3.Zero; // Возвращаем нулевой вектор, если пересечения с треугольником нет
    }
}