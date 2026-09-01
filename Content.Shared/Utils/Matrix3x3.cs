using System;

namespace Content.Shared.Utils;

public struct Matrix3x3
{
    public float M11, M12, M13;
    public float M21, M22, M23;
    public float M31, M32, M33;
    
    public Matrix3x3(
        float m11, float m12, float m13,
        float m21, float m22, float m23,
        float m31, float m32, float m33)
    {
        M11 = m11; M12 = m12; M13 = m13;
        M21 = m21; M22 = m22; M23 = m23;
        M31 = m31; M32 = m32; M33 = m33;
    }
    
    // --- Static Properties ---
    
    public static Matrix3x3 Identity => new Matrix3x3(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1
    );
    
    public static Matrix3x3 Zero => new Matrix3x3(
        0, 0, 0,
        0, 0, 0,
        0, 0, 0
    );
    
    // --- Matrix Properties ---

    public float Trace => M11 + M22 + M33;

    public float Determinant => 
        M11 * (M22 * M33 - M23 * M32) -
        M12 * (M21 * M33 - M23 * M31) +
        M13 * (M21 * M32 - M22 * M31);

    // --- Core Operations ---
    
    public static Matrix3x3 Transpose(Matrix3x3 m) => new Matrix3x3(
        m.M11, m.M21, m.M31,
        m.M12, m.M22, m.M32,
        m.M13, m.M23, m.M33
    );

    public static void Invert(Matrix3x3 m, out Matrix3x3 inverted)
    {
        inverted = Invert(m);
    }

    public static Matrix3x3 Invert(Matrix3x3 m)
    {
        float det = m.Determinant;
        if (MathF.Abs(det) < 1e-7f)
            throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

        float invDet = 1f / det;

        return new Matrix3x3(
            (m.M22 * m.M33 - m.M23 * m.M32) * invDet,
            (m.M13 * m.M32 - m.M12 * m.M33) * invDet,
            (m.M12 * m.M23 - m.M13 * m.M22) * invDet,

            (m.M23 * m.M31 - m.M21 * m.M33) * invDet,
            (m.M11 * m.M33 - m.M13 * m.M31) * invDet,
            (m.M13 * m.M21 - m.M11 * m.M23) * invDet,

            (m.M21 * m.M32 - m.M22 * m.M31) * invDet,
            (m.M12 * m.M31 - m.M11 * m.M32) * invDet,
            (m.M11 * m.M22 - m.M12 * m.M21) * invDet
        );
    }

    // --- Creation / Transformations ---
    
    public static Matrix3x3 CreateFromQuaternion(Quaternion q)
    {
        var xx = q.X * q.X;
        var yy = q.Y * q.Y;
        var zz = q.Z * q.Z;
        var xy = q.X * q.Y;
        var xz = q.X * q.Z;
        var yz = q.Y * q.Z;
        var wx = q.W * q.X;
        var wy = q.W * q.Y;
        var wz = q.W * q.Z;
        
        return new Matrix3x3(
            1f - 2f * (yy + zz), 2f * (xy - wz), 2f * (xz + wy),
            2f * (xy + wz), 1f - 2f * (xx + zz), 2f * (yz - wx),
            2f * (xz - wy), 2f * (yz + wx), 1f - 2f * (xx + yy)
        );
    }

    public static Matrix3x3 CreateFromAxisAngle(Vector3 axis, float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);
        float t = 1f - c;
        
        float x = axis.X, y = axis.Y, z = axis.Z;

        return new Matrix3x3(
            t * x * x + c,     t * x * y - s * z, t * x * z + s * y,
            t * x * y + s * z, t * y * y + c,     t * y * z - s * x,
            t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
    }

    public static Matrix3x3 CreateRotationX(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Matrix3x3(1, 0, 0, 0, c, -s, 0, s, c);
    }

    public static Matrix3x3 CreateRotationY(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Matrix3x3(c, 0, s, 0, 1, 0, -s, 0, c);
    }

    public static Matrix3x3 CreateRotationZ(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Matrix3x3(c, -s, 0, s, c, 0, 0, 0, 1);
    }

    public static Matrix3x3 CreateScale(float scale) => new Matrix3x3(
        scale, 0, 0,
        0, scale, 0,
        0, 0, scale);

    public static Matrix3x3 CreateScale(Vector3 scale) => new Matrix3x3(
        scale.X, 0, 0,
        0, scale.Y, 0,
        0, 0, scale.Z);
    
    public static Matrix3x3 SkewSymmetric(Vector3 v) => new(
        0, -v.Z,  v.Y,
        v.Z,  0, -v.X,
        -v.Y,  v.X, 0
    );

    // --- Transformations ---

    /// <summary>
    /// Transforms a Vector3 by this matrix.
    /// Functionally identical to the '*' operator but provides a more explicit, readable method call.
    /// </summary>
    public Vector3 Transform(Vector3 v)
    {
        return new Vector3(
            M11 * v.X + M12 * v.Y + M13 * v.Z,
            M21 * v.X + M22 * v.Y + M23 * v.Z,
            M31 * v.X + M32 * v.Y + M33 * v.Z
        );
    }

    /// <summary>
    /// Transforms a Vector3 by the transpose of this matrix.
    /// Highly useful in graphics/physics for transforming normal vectors when the matrix 
    /// contains non-uniform scaling (where the inverse-transpose is normally required).
    /// </summary>
    public Vector3 TransformTranspose(Vector3 v)
    {
        return new Vector3(
            M11 * v.X + M21 * v.Y + M31 * v.Z,
            M12 * v.X + M22 * v.Y + M32 * v.Z,
            M13 * v.X + M23 * v.Y + M33 * v.Z
        );
    }

    /// <summary>
    /// Static transformation method matching XNA/MonoGame conventions.
    /// </summary>
    public static Vector3 Transform(Vector3 v, Matrix3x3 m) => m * v;
    
    // --- Extraction ---

    public Vector3 GetRow(int row) => row switch
    {
        0 => new Vector3(M11, M12, M13),
        1 => new Vector3(M21, M22, M23),
        2 => new Vector3(M31, M32, M33),
        _ => throw new ArgumentOutOfRangeException(nameof(row))
    };

    public Vector3 GetColumn(int col) => col switch
    {
        0 => new Vector3(M11, M21, M31),
        1 => new Vector3(M12, M22, M32),
        2 => new Vector3(M13, M23, M33),
        _ => throw new ArgumentOutOfRangeException(nameof(col))
    };

    // --- Operators ---

    public static Vector3 operator *(Matrix3x3 m, Vector3 v)
    {
        return new Vector3(
            m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z,
            m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z,
            m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z
        );
    }
    
    public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
            
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
            
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33
        );
    }

    public static Matrix3x3 operator +(Matrix3x3 a, Matrix3x3 b) => new Matrix3x3(
        a.M11 + b.M11, a.M12 + b.M12, a.M13 + b.M13,
        a.M21 + b.M21, a.M22 + b.M22, a.M23 + b.M23,
        a.M31 + b.M31, a.M32 + b.M32, a.M33 + b.M33);

    public static Matrix3x3 operator -(Matrix3x3 a, Matrix3x3 b) => new Matrix3x3(
        a.M11 - b.M11, a.M12 - b.M12, a.M13 - b.M13,
        a.M21 - b.M21, a.M22 - b.M22, a.M23 - b.M23,
        a.M31 - b.M31, a.M32 - b.M32, a.M33 - b.M33);

    public static Matrix3x3 operator *(Matrix3x3 m, float s) => new Matrix3x3(
        m.M11 * s, m.M12 * s, m.M13 * s,
        m.M21 * s, m.M22 * s, m.M23 * s,
        m.M31 * s, m.M32 * s, m.M33 * s);

    public static Matrix3x3 operator *(float s, Matrix3x3 m) => m * s;

    // --- Equality & Formatting ---

    public override bool Equals(object? obj) => obj is Matrix3x3 other && Equals(other);

    public bool Equals(Matrix3x3 other) =>
        M11 == other.M11 && M12 == other.M12 && M13 == other.M13 &&
        M21 == other.M21 && M22 == other.M22 && M23 == other.M23 &&
        M31 == other.M31 && M32 == other.M32 && M33 == other.M33;

    /// <summary>
    /// Useful for comparing matrices in physics/game logic where floating point inaccuracies occur.
    /// </summary>
    public bool EqualsApprox(Matrix3x3 other, float epsilon = 1e-5f) =>
        MathF.Abs(M11 - other.M11) < epsilon && MathF.Abs(M12 - other.M12) < epsilon && MathF.Abs(M13 - other.M13) < epsilon &&
        MathF.Abs(M21 - other.M21) < epsilon && MathF.Abs(M22 - other.M22) < epsilon && MathF.Abs(M23 - other.M23) < epsilon &&
        MathF.Abs(M31 - other.M31) < epsilon && MathF.Abs(M32 - other.M32) < epsilon && MathF.Abs(M33 - other.M33) < epsilon;

    public static bool operator ==(Matrix3x3 a, Matrix3x3 b) => a.Equals(b);
    public static bool operator !=(Matrix3x3 a, Matrix3x3 b) => !a.Equals(b);

    public override int GetHashCode() 
    {
        var hash = new HashCode();
        hash.Add(M11);
        hash.Add(M12);
        hash.Add(M13);
        
        hash.Add(M21);
        hash.Add(M22);
        hash.Add(M23);
        
        hash.Add(M31);
        hash.Add(M32);
        hash.Add(M33);
        
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"Matrix3x3(\n  [{M11}, {M12}, {M13}],\n  [{M21}, {M22}, {M23}],\n  [{M31}, {M32}, {M33}]\n)";
}