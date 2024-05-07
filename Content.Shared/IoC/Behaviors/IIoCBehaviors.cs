namespace Content.Shared.IoC.Behaviors;

public interface IIoCBehaviors
{
    void AddBehavior((Type interfaceType, Type implementation) b);
    void Initialize<T>();
    void Resolve();
}