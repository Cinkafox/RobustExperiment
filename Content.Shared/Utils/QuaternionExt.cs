namespace Content.Shared.Utils;

using System;
using System.Runtime.CompilerServices;

public static class QuaternionExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(this EulerAngles e)
    {
        var pitch = (float)e.Pitch; // X
        var yaw   = (float)e.Yaw;   // Y
        var roll  = (float)e.Roll;  // Z

        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll);

        return Quaternion.Normalize(qy * qx * qz);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EulerAngles ToEulerAngle(this Quaternion q)
    {
        q = Quaternion.Normalize(q);

        // Extract Pitch (X)
        var sinp = 2f * (q.W * q.X - q.Y * q.Z);
        float pitch;

        if (MathF.Abs(sinp) >= 1f)
            pitch = MathF.CopySign(MathF.PI / 2f, sinp); // gimbal lock
        else
            pitch = MathF.Asin(sinp);

        // Extract Yaw (Y)
        var siny = 2f * (q.W * q.Y + q.Z * q.X);
        var cosy = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        var yaw = MathF.Atan2(siny, cosy);

        // Extract Roll (Z)
        var sinr = 2f * (q.W * q.Z + q.X * q.Y);
        var cosr = 1f - 2f * (q.Z * q.Z + q.X * q.X);
        var roll = MathF.Atan2(sinr, cosr);

        return new EulerAngles(pitch, yaw, roll);
    }
}