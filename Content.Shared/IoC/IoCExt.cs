using Content.Shared.IoC.Behaviors;
using Robust.Shared.Reflection;

namespace Content.Shared.IoC;

public static class IoCExt
{
    public static readonly IIoCBehaviors Behaviors = new IoCBehaviors();

    public static void AutoRegisterWithAttr()
    {
        var reg = IoCManager.Resolve<IReflectionManager>().FindTypesWithAttribute<IoCRegisterAttribute>();
        foreach (var iocReg in reg)
        {
            var attr = (IoCRegisterAttribute) Attribute.GetCustomAttribute(iocReg, typeof(IoCRegisterAttribute))!;

            var interf = iocReg;
            if (attr.Interface is not null)
                interf = attr.Interface;

            IoCManager.Instance?.Register(interf,iocReg);

            Behaviors.AddBehavior((interf,iocReg));
        }
    }
}


