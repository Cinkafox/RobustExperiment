namespace Content.Shared.Utils;

using System;
using System.Runtime.CompilerServices;

public static class QuaternionExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(this EulerAngles eulerAngles)
    {
        var roll  = (float)eulerAngles.Roll;
        var pitch = (float)eulerAngles.Pitch;
        var yaw   = (float)eulerAngles.Yaw;

        var halfRoll  = roll * 0.5f;
        var halfPitch = pitch * 0.5f;
        var halfYaw   = yaw * 0.5f;

        var sinR = MathF.Sin(halfRoll);
        var cosR = MathF.Cos(halfRoll);
        var sinP = MathF.Sin(halfPitch);
        var cosP = MathF.Cos(halfPitch);
        var sinY = MathF.Sin(halfYaw);
        var cosY = MathF.Cos(halfYaw);
        
        var w = cosR * cosP * cosY + sinR * sinP * sinY;
        var x = sinR * cosP * cosY - cosR * sinP * sinY;
        var y = cosR * sinP * cosY + sinR * cosP * sinY;
        var z = cosR * cosP * sinY - sinR * sinP * cosY;

        return new Quaternion(x, y, z, w);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EulerAngles ToEulerAngle(this Quaternion q)
    {
        var w = q.W;
        var x = q.X;
        var y = q.Y;
        var z = q.Z;
        
        var sinRcosP = 2.0f * (w * x + y * z);
        var cosRcosP = 1.0f - 2.0f * (x * x + y * y);
        var roll = MathF.Atan2(sinRcosP, cosRcosP);
        
        var sinPitch = 2.0f * (w * y - z * x);
        sinPitch = Math.Clamp(sinPitch, -1.0f, 1.0f);
        var pitch = MathF.Asin(sinPitch);
        
        var sinYcosP = 2.0f * (w * z + x * y);
        var cosYcosP = 1.0f - 2.0f * (y * y + z * z);
        var yaw = MathF.Atan2(sinYcosP, cosYcosP);
        
        return new EulerAngles(pitch, yaw, roll);
    }
}