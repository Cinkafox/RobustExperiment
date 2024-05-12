using Content.Client.Font;
using Content.Client.StyleSheet.Dynamic;
using Content.Shared.IoC;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.StyleSheet;

[IoCRegister]
public sealed class StyleSheetManager : IPostInitializeBehavior
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public void ApplySheet(string prototype)
    {
        if(!_prototypeManager.TryIndex<StyleSheetPrototype>(prototype,out var proto))
            return;
        ApplySheet(proto);
    }

    public void ApplySheet(StyleSheetPrototype stylePrototype)
    {
        _userInterfaceManager.Stylesheet = new Stylesheet(GetRules(stylePrototype));
        DebSomeShit(GetRules(stylePrototype));
    }

    public List<StyleRule> GetRules(StyleSheetPrototype stylePrototype)
    {
        List<StyleRule> styleRule;
        
        if (stylePrototype.Parent is not null && _prototypeManager.TryIndex(stylePrototype.Parent, out var parentProto))
            styleRule = GetRules(parentProto);
        else
            styleRule = new List<StyleRule>();
        
        foreach (var (elementPath, value) in stylePrototype.Styles)
        {
            var element = GetElement(elementPath, stylePrototype);
            foreach (var (key,dynamicValue) in value)
            {
                element.Prop(key, dynamicValue.GetValueObject());
            }
            
            styleRule.Add(element);
        }

        return styleRule;
    }
    
    public MutableSelectorElement GetElement(string type,StyleSheetPrototype prototype)
    {
        var pseudoSeparator = type.Split("#");
        
        var classSeparator = pseudoSeparator[0].Split(".");
        var definedType = classSeparator[0];
        var element = new MutableSelectorElement();

        if (definedType != "*" && !string.IsNullOrEmpty(definedType))
        {
            if (prototype.TypeDefinition.TryGetValue(definedType, out var definition))
            {
                definedType = definition;
            }
            
            element.Type = _reflectionManager.GetType(definedType);
        }
        Logger.Debug(element.Type?.FullName + " ASS?? " + definedType);
        
        
        for (var i = 1; i < classSeparator.Length; i++)
        {
            element.Class(classSeparator[i]);
        }
        for (var i = 1; i < pseudoSeparator.Length; i++)
        {
            element.Pseudo(pseudoSeparator[i]);
        }
        
        return element;
    }

    public void DebSomeShit(List<StyleRule> rules)
    {
        Logger.Debug("DEBUGING STYLE:");
        foreach (var rule in rules)
        {
            Logger.Debug($"  PROPERTIES:");
            foreach (var prop in rule.Properties)
            {
                Logger.Debug($"   - NAME:{prop.Name} TYPE:{prop.Value.GetType().FullName}");
            }
            Logger.Debug($"  SPECIFICITY: CS:{rule.Specificity.ClassSelectors} ID:{rule.Specificity.IdSelectors} TYPE:{rule.Specificity.TypeSelectors}");
            Logger.Debug($"  TYPE OF SHIT:{rule.Selector.GetType()}");
            if (rule.Selector is SelectorElement selector)
            {
                Logger.Debug($"  SELECTOR: {selector.ElementId} {selector.ElementType?.FullName}");

                if(selector.ElementClasses is not null)
                {
                    Logger.Debug("  ELEMENT CLASSES:");
                    foreach (var classes in selector.ElementClasses)
                    {
                        Logger.Debug($"    - {classes}");
                    }
                }
                
                if(selector.PseudoClasses is not null)
                {
                    Logger.Debug("  ELEMENT PSEUDO:");
                    foreach (var classes in selector.PseudoClasses)
                    {
                        Logger.Debug($"    - {classes}");
                    }
                }
            }
            Logger.Debug("_______________________________________________");
        }
        
    }

    public void PostInitialize()
    {
        //ApplySheet("default");
    }
}