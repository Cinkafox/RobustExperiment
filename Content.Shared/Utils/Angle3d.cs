namespace Content.Shared.Utils;

public readonly struct Angle3d : IApproxEquatable<Angle3d>, IEquatable<Angle3d>
{
    public readonly Angle Pitch = Angle.Zero;
    public readonly Angle Yaw = Angle.Zero;
    public readonly Angle Roll = Angle.Zero;

    public Angle3d()
    {
    }
    
    public Angle3d(Angle pitch, Angle yaw, Angle roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public static Angle3d operator +(Angle3d one, Angle3d two)
    {
        return new Angle3d(one.Pitch + two.Pitch, one.Yaw + two.Yaw, one.Roll + two.Roll);
    }
    
    public static Angle3d operator -(Angle3d one, Angle3d two)
    {
        return new Angle3d(one.Pitch - two.Pitch, one.Yaw - two.Yaw, one.Roll - two.Roll);
    }

    public bool EqualsApprox(Angle3d other)
    {
        return Pitch.EqualsApprox(other.Pitch) && Yaw.EqualsApprox(other.Yaw) && Roll.EqualsApprox(other.Roll);
    }

    public bool EqualsApprox(Angle3d other, double tolerance)
    {
        return Pitch.EqualsApprox(other.Pitch,tolerance) && Yaw.EqualsApprox(other.Yaw,tolerance) && Roll.EqualsApprox(other.Roll,tolerance);
    }

    public bool Equals(Angle3d other)
    {
        return Pitch.Equals(other.Pitch) && Yaw.Equals(other.Yaw) && Roll.Equals(other.Roll);
    }

    public override string ToString()
    {
        return $"PITCH:{Pitch} YAW:{Yaw} ROLL:{Roll}";
    }
}