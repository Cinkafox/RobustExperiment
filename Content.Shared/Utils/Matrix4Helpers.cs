using System.Numerics;
using System.Runtime.CompilerServices;

namespace Content.Shared.Utils;

public static class Matrix4Helpers
{
    public static bool EqualsApprox(this Matrix4x4 a, Matrix4x4 b, float tolerance = 1e-6f)
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
    public static bool EqualsApprox(this Matrix4x4 a, Matrix4x4 b, double tolerance)
    {
        return a.EqualsApprox(b, (float)tolerance);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransform(float posX, float posY, float posZ, float x, float y, float z, float w, float scaleX = 1, float scaleY = 1, float scaleZ = 1)
    {
        float xx = x * x;
        float yy = y * y;
        float zz = z * z;

        float xy = x * y;
        float wz = z * w;
        float xz = z * x;
        float wy = y * w;
        float yz = y * z;
        float wx = x * w;
        
        return new Matrix4x4
        {
            M11 = (1.0f - 2.0f * (yy + zz)) * scaleX,
            M12 = 2.0f * (xy + wz) * scaleX,
            M13 = 2.0f * (xz - wy),
            M14 = 0,
            M21 = 2.0f * (xy - wz) * scaleY,
            M22 = (1.0f - 2.0f * (zz + xx)) * scaleY,
            M23 = 2.0f * (yz + wx) * scaleY,
            M24 = 0,
            M31 = 2.0f * (xz + wy) * scaleZ,
            M32 = 2.0f * (yz - wx) * scaleZ,
            M33 = (1.0f - 2.0f * (yy + xx)) * scaleZ,
            M34 = 0,
            M41 = posX,
            M42 = posY,
            M43 = posZ,
            M44 = 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateInverseTransform(float posX, float posY, float posZ, float x, float y, float z, float w, float scaleX = 1, float scaleY = 1, float scaleZ = 1)
    {
        // Invert the scale
        float invScaleX = 1.0f / scaleX;
        float invScaleY = 1.0f / scaleY;
        float invScaleZ = 1.0f / scaleZ;

        // Conjugate the quaternion for inverse rotation
        float invX = -x;
        float invY = -y;
        float invZ = -z;
        float invW = w;

        float xx = invX * invX;
        float yy = invY * invY;
        float zz = invZ * invZ;

        float xy = invX * invY;
        float wz = invZ * invW;
        float xz = invZ * invX;
        float wy = invY * invW;
        float yz = invY * invZ;
        float wx = invX * invW;

        // Construct the inverse rotation matrix scaled by the inverse of the scale factors
        var rotationInv = new Matrix4x4
        {
            M11 = (1.0f - 2.0f * (yy + zz)) * invScaleX,
            M12 = 2.0f * (xy - wz) * invScaleX,
            M13 = 2.0f * (xz + wy) * invScaleX,
            M14 = 0,
            M21 = 2.0f * (xy + wz) * invScaleY,
            M22 = (1.0f - 2.0f * (zz + xx)) * invScaleY,
            M23 = 2.0f * (yz - wx) * invScaleY,
            M24 = 0,
            M31 = 2.0f * (xz - wy) * invScaleZ,
            M32 = 2.0f * (yz + wx) * invScaleZ,
            M33 = (1.0f - 2.0f * (yy + xx)) * invScaleZ,
            M34 = 0,
            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };

        // Calculate the inverse translation
        float invPosX = -posX;
        float invPosY = -posY;
        float invPosZ = -posZ;

        // Apply inverse translation to the rotation matrix
        rotationInv.M41 = invPosX * rotationInv.M11 + invPosY * rotationInv.M21 + invPosZ * rotationInv.M31;
        rotationInv.M42 = invPosX * rotationInv.M12 + invPosY * rotationInv.M22 + invPosZ * rotationInv.M32;
        rotationInv.M43 = invPosX * rotationInv.M13 + invPosY * rotationInv.M23 + invPosZ * rotationInv.M33;

        return rotationInv;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransform(Vector3 pos, Quaternion quaternion, Vector3 scale)
    {
        quaternion = Quaternion.Normalize(quaternion);
        return CreateTransform(pos.X, pos.Y, pos.Z, quaternion.X, quaternion.Y, quaternion.Z, quaternion.W, scale.X, scale.Y, scale.Z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateInverseTransform(Vector3 pos, Quaternion quaternion, Vector3 scale)
    {
        quaternion = Quaternion.Normalize(quaternion);
        return CreateInverseTransform(pos.X, pos.Y, pos.Z, quaternion.X, quaternion.Y, quaternion.Z, quaternion.W, scale.X, scale.Y, scale.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTranslation(Vector3 pos)
    {
        return CreateTranslation(pos.X, pos.Y, pos.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTranslation(float x, float y, float z)
    {
        return new Matrix4x4
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
    public static Matrix4x4 CreateRotationX(double angle)
    {
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        return new Matrix4x4
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
    public static Matrix4x4 CreateRotationY(double angle)
    {
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        return new Matrix4x4
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
    public static Matrix4x4 CreateRotationZ(double angle)
    {
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        return new Matrix4x4
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
    public static Matrix4x4 CreateScale(float x, float y, float z)
    {
        return new Matrix4x4
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
    public static Matrix4x4 CreateRotation(Quaternion quaternion)
    {
        quaternion = Quaternion.Normalize(quaternion);
        
        float x = quaternion.X;
        float y = quaternion.Y;
        float z = quaternion.Z;
        float w = quaternion.W;
        
        float xx = x * x;
        float yy = y * y;
        float zz = z * z;
        float xy = x * y;
        float xz = x * z;
        float yz = y * z;
        float wx = w * x;
        float wy = w * y;
        float wz = w * z;

        return new Matrix4x4(
            1.0f - 2.0f * (yy + zz), 2.0f * (xy - wz), 2.0f * (xz + wy), 0.0f,
            2.0f * (xy + wz), 1.0f - 2.0f * (xx + zz), 2.0f * (yz - wx), 0.0f,
            2.0f * (xz - wy), 2.0f * (yz + wx), 1.0f - 2.0f * (xx + yy), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateRotation(EulerAngles rotation)
    {
        return CreateRotation(rotation.ToQuaternion());
        
        var pitchRad = rotation.Pitch.Theta;
        var yawRad = rotation.Yaw.Theta;
        var rollRad = rotation.Roll.Theta;
        
        float sinPitch = (float)Math.Sin(pitchRad);
        float cosPitch = (float)Math.Cos(pitchRad);
        float sinYaw = (float)Math.Sin(yawRad);
        float cosYaw = (float)Math.Cos(yawRad);
        float sinRoll = (float)Math.Sin(rollRad);
        float cosRoll = (float)Math.Cos(rollRad);
        
        return new Matrix4x4(
            cosYaw * cosRoll, cosYaw * sinRoll * sinPitch - sinYaw * cosPitch, cosYaw * sinRoll * cosPitch + sinYaw * sinPitch, 0.0f,
            sinYaw * cosRoll, sinYaw * sinRoll * sinPitch + cosYaw * cosPitch, sinYaw * sinRoll * cosPitch - cosYaw * sinPitch, 0.0f,
            -sinRoll, cosRoll * sinPitch, cosRoll * cosPitch, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
    }
}