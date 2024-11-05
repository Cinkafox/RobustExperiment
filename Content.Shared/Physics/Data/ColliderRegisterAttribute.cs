namespace Content.Shared.Physics.Data;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ColliderRegisterAttribute : Attribute
{
    public Type A;
    public Type B;


    public ColliderRegisterAttribute(Type a, Type b)
    {
        A = a;
        B = b;
    }
}