using System.Collections;
using System.Numerics;

namespace Content.Client.Utils;

public sealed class Triangle : IEnumerable<Vector4>
{
    private Vector3 vertexNormal1;
    private Vector3 vertexNormal2;
    private Vector3 vertexNormal3;
    private bool hasVertexNormal;
    
    public Vector4 p1;
    public Vector4 p2;
    public Vector4 p3;

    public float Z => (p1.Z + p2.Z + p3.Z) * (1.0f / 3.0f);

    public void Transform(Matrix4x4 matrix4)
    {
        p1 = Vector4.Transform(p1, matrix4);
        p2 = Vector4.Transform(p2, matrix4);
        p3 = Vector4.Transform(p3, matrix4);
    }

    public void SetP1(Vector3 point) => SetVec(ref p1, point);
    public void SetP2(Vector3 point) => SetVec(ref p2, point);
    public void SetP3(Vector3 point) => SetVec(ref p3, point);
    public Vector3 GetP1() => GetVec(p1);
    public Vector3 GetP2() => GetVec(p2);
    public Vector3 GetP3() => GetVec(p3);
    
    private Vector3 GetVec(Vector4 vector) => new Vector3(vector.X, vector.Y, vector.Z);

    private void SetVec(ref Vector4 ord ,Vector3 value)
    {
        ord.X = value.X;
        ord.Y = value.Y;
        ord.Z = value.Z;
        ord.W = 1f;
    }

    public void SetVertexNormal(Vector3 vn1, Vector3 vn2, Vector3 vn3)
    {
        vertexNormal1 = vn1;
        vertexNormal2 = vn2;
        vertexNormal3 = vn3;
        
        hasVertexNormal = true;
    }

    public Vector3 Normal()
    {
        var u = p2 - p1;
        var v = p3 - p1; 
        
        var nx = u.Y * v.Z - u.Z * v.Y;
        var ny = u.Z * v.X - u.X * v.Z;
        var nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }

    public void Clear()
    {
        p1 = p2 = p3 = new Vector4(0,0,0,1);
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