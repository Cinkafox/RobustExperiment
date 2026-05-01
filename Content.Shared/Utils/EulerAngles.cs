namespace Content.Shared.Utils;

public readonly struct EulerAngles : IApproxEquatable<EulerAngles>, IEquatable<EulerAngles>
{
    public readonly Angle Pitch = Angle.Zero;
    public readonly Angle Yaw = Angle.Zero;
    public readonly Angle Roll = Angle.Zero;
    
    public static readonly EulerAngles Zero = new();

    public EulerAngles() { }
    
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

    public static EulerAngles operator *(EulerAngles one, float two)
    {
        return new EulerAngles(one.Pitch * two, one.Yaw * two, one.Roll * two);
    }
    
    public bool Equals(EulerAngles other)
    {
        return Pitch.Equals(other.Pitch) && Yaw.Equals(other.Yaw) && Roll.Equals(other.Roll);
    }

    public bool EqualsApprox(EulerAngles other)
    {
        return Pitch.EqualsApprox(other.Pitch) && Yaw.EqualsApprox(other.Yaw) && Roll.EqualsApprox(other.Roll);
    }

    public bool EqualsApprox(EulerAngles other, double tolerance)
    {
        return Pitch.EqualsApprox(other.Pitch, tolerance) && Yaw.EqualsApprox(other.Yaw, tolerance) && Roll.EqualsApprox(other.Roll, tolerance);
    }
    
    public override bool Equals(object? obj) => obj is EulerAngles other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Pitch, Yaw, Roll);

    public static bool operator ==(EulerAngles left, EulerAngles right) => left.Equals(right);
    public static bool operator !=(EulerAngles left, EulerAngles right) => !(left == right);

    public override string ToString()
    {
        return $"PITCH:{Pitch.Degrees:F2} YAW:{Yaw.Degrees:F2} ROLL:{Roll.Degrees:F2}";
    }
}