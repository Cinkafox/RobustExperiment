using System.Numerics;

namespace Content.Shared.Utils;

public readonly struct EulerAngles : IApproxEquatable<EulerAngles>, IEquatable<EulerAngles>
{
    public readonly Angle Pitch = Angle.Zero;
    public readonly Angle Yaw = Angle.Zero;
    public readonly Angle Roll = Angle.Zero;
    public Matrix4x4 Matrix => Matrix4Helpers.CreateRotation(this);
    
    public static EulerAngles Zero = new EulerAngles();

    public EulerAngles()
    {
    }
    
    public EulerAngles(Angle pitch, Angle yaw, Angle roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public static EulerAngles operator +(EulerAngles one, EulerAngles two)
    {
        return new EulerAngles(one.Pitch + two.Pitch, one.Yaw + two.Yaw, one.Roll + two.Roll);
    }
    
    public static EulerAngles operator -(EulerAngles one, EulerAngles two)
    {
        return new EulerAngles(one.Pitch - two.Pitch, one.Yaw - two.Yaw, one.Roll - two.Roll);
    }

    public bool EqualsApprox(EulerAngles other)
    {
        return Pitch.EqualsApprox(other.Pitch) && Yaw.EqualsApprox(other.Yaw) && Roll.EqualsApprox(other.Roll);
    }

    public bool EqualsApprox(EulerAngles other, double tolerance)
    {
        return Pitch.EqualsApprox(other.Pitch,tolerance) && Yaw.EqualsApprox(other.Yaw,tolerance) && Roll.EqualsApprox(other.Roll,tolerance);
    }

    public bool Equals(EulerAngles other)
    {
        return Pitch.Equals(other.Pitch) && Yaw.Equals(other.Yaw) && Roll.Equals(other.Roll);
    }

    public override string ToString()
    {
        return $"PITCH:{Pitch.Degrees} YAW:{Yaw.Degrees} ROLL:{Roll.Degrees}";
    }

    public Vector3 ToVec()
    {
        return RotateVec(Vector3.UnitX);
    }

    public Vector3 RotateVec(Vector3 pos)
    {
        return Vector3.Transform(pos, Matrix);
    }
    
    public Quaternion ToQuaternion()
    {
        var pitchRad = Pitch.Theta;
        var yawRad = Yaw.Theta;
        var rollRad = Roll.Theta;
        
        float cy = (float)Math.Cos(yawRad * 0.5);
        float sy = (float)Math.Sin(yawRad * 0.5);
        float cp = (float)Math.Cos(pitchRad * 0.5);
        float sp = (float)Math.Sin(pitchRad * 0.5);
        float cr = (float)Math.Cos(rollRad * 0.5);
        float sr = (float)Math.Sin(rollRad * 0.5);

        var q = new Quaternion
        {
            W = cr * cp * cy + sr * sp * sy,
            X = sr * cp * cy - cr * sp * sy,
            Y = cr * sp * cy + sr * cp * sy,
            Z = cr * cp * sy - sr * sp * cy
        };

        return q;
    }
}

public static class QuaternionExt
{
    
    public static EulerAngles ToEulerAngle(this Quaternion q)
    {
        q = Quaternion.Normalize(q);
        
        float x = q.X;
        float y = q.Y;
        float z = q.Z;
        float w = q.W;

        float sinr_cosp = 2 * (w * x + y * z);
        float cosr_cosp = 1 - 2 * (x * x + y * y);
        float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

        float sinp = 2 * (w * y - z * x);
        float pitch;
        if (Math.Abs(sinp) >= 1)
            pitch = (float)Math.CopySign(Math.PI / 2, sinp);
        else
            pitch = (float)Math.Asin(sinp);

        float siny_cosp = 2 * (w * z + x * y);
        float cosy_cosp = 1 - 2 * (y * y + z * z);
        float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

        if (float.IsNaN(pitch)) pitch = 0;
        if (float.IsNaN(yaw)) yaw = 0;
        if (float.IsNaN(roll)) roll = 0;
        
        return new(pitch, yaw, roll);
    }
}