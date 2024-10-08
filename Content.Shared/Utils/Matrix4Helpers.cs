using System.Numerics;
using System.Runtime.CompilerServices;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Utils;

public static class Matrix4Helpers
{
    public static bool EqualsApprox(this Matrix4 a, Matrix4 b, float tolerance = 1e-6f)
    {
        return
            Math.Abs(a.M11 - b.M11) <= tolerance &&
            Math.Abs(a.M12 - b.M12) <= tolerance &&
            Math.Abs(a.M13 - b.M13) <= tolerance &&
            Math.Abs(a.M14 - b.M14) <= tolerance &&
            Math.Abs(a.M21 - b.M21) <= tolerance &&
            Math.Abs(a.M22 - b.M22) <= tolerance &&
            Math.Abs(a.M23 - b.M23) <= tolerance &&
            Math.Abs(a.M24 - b.M24) <= tolerance &&
            Math.Abs(a.M31 - b.M31) <= tolerance &&
            Math.Abs(a.M32 - b.M32) <= tolerance &&
            Math.Abs(a.M33 - b.M33) <= tolerance &&
            Math.Abs(a.M34 - b.M34) <= tolerance &&
            Math.Abs(a.M41 - b.M41) <= tolerance &&
            Math.Abs(a.M42 - b.M42) <= tolerance &&
            Math.Abs(a.M43 - b.M43) <= tolerance &&
            Math.Abs(a.M44 - b.M44) <= tolerance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsApprox(this Matrix4 a, Matrix4 b, double tolerance)
    {
        return a.EqualsApprox(b, (float)tolerance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateTransform(float posX, float posY, float posZ, double angleX, double angleY, double angleZ, float scaleX = 1, float scaleY = 1, float scaleZ = 1)
    {
        // Вычисляем синусы и косинусы для каждого угла
        var sinX = (float)Math.Sin(angleX);
        var cosX = (float)Math.Cos(angleX);
        var sinY = (float)Math.Sin(angleY);
        var cosY = (float)Math.Cos(angleY);
        var sinZ = (float)Math.Sin(angleZ);
        var cosZ = (float)Math.Cos(angleZ);

        // Применяем комбинацию вращений по X, Y и Z
        return new Matrix4
        {
            M11 = cosY * cosZ * scaleX,
            M12 = (cosY * sinZ) * scaleX,
            M13 = -sinY * scaleX,
            M14 = 0,
            M21 = (sinX * sinY * cosZ - cosX * sinZ) * scaleY,
            M22 = (sinX * sinY * sinZ + cosX * cosZ) * scaleY,
            M23 = sinX * cosY * scaleY,
            M24 = 0,
            M31 = (cosX * sinY * cosZ + sinX * sinZ) * scaleZ,
            M32 = (cosX * sinY * sinZ - sinX * cosZ) * scaleZ,
            M33 = cosX * cosY * scaleZ,
            M34 = 0,
            M41 = posX,
            M42 = posY,
            M43 = posZ,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateInverseTransform(float posX, float posY, float posZ, double angleX, double angleY, double angleZ, float scaleX = 1, float scaleY = 1, float scaleZ = 1)
    {
        // Вычисляем синусы и косинусы для каждого угла
        var sinX = (float)Math.Sin(angleX);
        var cosX = (float)Math.Cos(angleX);
        var sinY = (float)Math.Sin(angleY);
        var cosY = (float)Math.Cos(angleY);
        var sinZ = (float)Math.Sin(angleZ);
        var cosZ = (float)Math.Cos(angleZ);

        // Применяем комбинацию инверсных вращений по X, Y и Z
        return new Matrix4
        {
            M11 = cosY * cosZ / scaleX,
            M12 = (sinX * sinY * cosZ - cosX * sinZ) / scaleY,
            M13 = (cosX * sinY * cosZ + sinX * sinZ) / scaleZ,
            M14 = 0,
            M21 = cosY * sinZ / scaleX,
            M22 = (sinX * sinY * sinZ + cosX * cosZ) / scaleY,
            M23 = (cosX * sinY * sinZ - sinX * cosZ) / scaleZ,
            M24 = 0,
            M31 = -sinY / scaleX,
            M32 = sinX * cosY / scaleY,
            M33 = cosX * cosY / scaleZ,
            M34 = 0,
            M41 = -(posX * cosY * cosZ + posY * cosY * sinZ - posZ * sinY) / scaleX,
            M42 = -(posX * (sinX * sinY * cosZ - cosX * sinZ) + posY * (sinX * sinY * sinZ + cosX * cosZ) - posZ * sinX * cosY) / scaleY,
            M43 = -(posX * (cosX * sinY * cosZ + sinX * sinZ) + posY * (cosX * sinY * sinZ - sinX * cosZ) - posZ * cosX * cosY) / scaleZ,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateTransform(Vector3 pos, Angle3d angle, Vector3 scale)
    {
        return CreateTransform(pos.X, pos.Y, pos.Z, angle.Pitch, angle.Yaw, angle.Roll, scale.X, scale.Y, scale.Z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateInverseTransform(Vector3 pos, Angle3d angle, Vector3 scale)
    {
        return CreateInverseTransform(pos.X, pos.Y, pos.Z, angle.Pitch, angle.Yaw, angle.Roll, scale.X, scale.Y, scale.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateTranslation(Vector3 pos)
    {
        return CreateTranslation(pos.X, pos.Y, pos.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateTranslation(float x, float y, float z)
    {
        return new Matrix4
        {
            M11 = 1,
            M12 = 0,
            M13 = 0,
            M14 = 0,
            M21 = 0,
            M22 = 1,
            M23 = 0,
            M24 = 0,
            M31 = 0,
            M32 = 0,
            M33 = 1,
            M34 = 0,
            M41 = x,
            M42 = y,
            M43 = z,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateRotationX(double angle)
    {
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        return new Matrix4
        {
            M11 = 1,
            M12 = 0,
            M13 = 0,
            M14 = 0,
            M21 = 0,
            M22 = cos,
            M23 = sin,
            M24 = 0,
            M31 = 0,
            M32 = -sin,
            M33 = cos,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateRotationY(double angle)
    {
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        return new Matrix4
        {
            M11 = cos,
            M12 = 0,
            M13 = -sin,
            M14 = 0,
            M21 = 0,
            M22 = 1,
            M23 = 0,
            M24 = 0,
            M31 = sin,
            M32 = 0,
            M33 = cos,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateRotationZ(double angle)
    {
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        return new Matrix4
        {
            M11 = cos,
            M12 = sin,
            M13 = 0,
            M14 = 0,
            M21 = -sin,
            M22 = cos,
            M23 = 0,
            M24 = 0,
            M31 = 0,
            M32 = 0,
            M33 = 1,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateScale(float x, float y, float z)
    {
        return new Matrix4
        {
            M11 = x,
            M12 = 0,
            M13 = 0,
            M14 = 0,
            M21 = 0,
            M22 = y,
            M23 = 0,
            M24 = 0,
            M31 = 0,
            M32 = 0,
            M33 = z,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateRotation(Angle3d transformLocalRotation)
    {
        var angleX = transformLocalRotation.Pitch;
        var angleY = transformLocalRotation.Yaw;
        var angleZ = transformLocalRotation.Roll;
        
        // Вычисляем синусы и косинусы для каждого угла
        var sinX = (float)Math.Sin(angleX);
        var cosX = (float)Math.Cos(angleX);
        var sinY = (float)Math.Sin(angleY);
        var cosY = (float)Math.Cos(angleY);
        var sinZ = (float)Math.Sin(angleZ);
        var cosZ = (float)Math.Cos(angleZ);

        // Применяем комбинацию вращений по X, Y и Z
        return new Matrix4
        {
            M11 = cosY * cosZ,
            M12 = (cosY * sinZ),
            M13 = -sinY,
            M14 = 0,
            M21 = (sinX * sinY * cosZ - cosX * sinZ),
            M22 = (sinX * sinY * sinZ + cosX * cosZ),
            M23 = sinX * cosY,
            M24 = 0,
            M31 = (cosX * sinY * cosZ + sinX * sinZ),
            M32 = (cosX * sinY * sinZ - sinX * cosZ),
            M33 = cosX * cosY,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
    }
}