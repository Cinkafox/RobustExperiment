namespace Content.Shared.Utils;
using System;

public static class QuaternionExt
{
    public static Quaternion ToQuaternion(this EulerAngles eulerAngles)
    {
        double roll = eulerAngles.Roll;
        double pitch = eulerAngles.Pitch;
        double yaw = eulerAngles.Yaw;

        var halfRoll = (float)roll / 2.0f;
        var halfPitch = (float)pitch / 2.0f;
        var halfYaw = (float)yaw / 2.0f;
        
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
    
    public static EulerAngles ToEulerAngle(this Quaternion q)
    {
        double w = q.W;
        double x = q.X;
        double y = q.Y;
        double z = q.Z;
        
        var sinRcosP = 2 * (w * x + y * z);
        var cosRcosP = 1 - 2 * (x * x + y * y);
        var roll = Math.Atan2(sinRcosP, cosRcosP);
        
        var sinPitch = 2 * (w * y - z * x);
        sinPitch = Math.Min(Math.Max(sinPitch, -1.0), 1.0);
        var pitch = Math.Asin(sinPitch);
        
        var sinYcosP = 2 * (w * z + x * y);
        var cosYcosP = 1 - 2 * (y * y + z * z);
        var yaw = Math.Atan2(sinYcosP, cosYcosP);

        return new EulerAngles(pitch, yaw, roll);
    }
}
