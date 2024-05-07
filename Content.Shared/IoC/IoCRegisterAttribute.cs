namespace Content.Shared.IoC;


[AttributeUsage(AttributeTargets.Class)]
public sealed class IoCRegisterAttribute : Attribute
{
    public Type? Interface;

    public IoCRegisterAttribute()
    {
    }

    public IoCRegisterAttribute(Type? @interface)
    {
        Interface = @interface;
    }
}