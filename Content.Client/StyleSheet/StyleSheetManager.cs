using Content.Shared.IoC;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.StyleSheet;

[IoCRegister]
public sealed class StyleSheetManager : IPostInitializeBehavior
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public void ApplySheet(string prototype)
    {
        if(!_prototypeManager.TryIndex<StyleSheetPrototype>(prototype,out var proto))
            return;
        ApplySheet(proto);
    }

    public void ApplySheet(StyleSheetPrototype stylePrototype)
    {
        var styleRule = new List<StyleRule>()
        {
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat()
            {
                BackgroundColor = Color.FromHex("#224466"),
                BorderColor = Color.Silver,
                BorderThickness = new Thickness(40)
            })
        };
        
        styleRule.AddRange(_userInterfaceManager.Stylesheet!.Rules);
        
        foreach (var (elementPath, value) in stylePrototype.Styles)
        {
            var element = GetElement(elementPath);
            foreach (var styleProt in value)
            {
                switch (styleProt.Act)
                {
                    case StyleAct.Prop:
                        Logger.Debug("PROP " + styleProt.Value.Value.GetType().FullName);
                        element.Prop(styleProt.Key, styleProt.Value.Value);
                        break;
                    case StyleAct.Class:
                        element.Class(styleProt.Key);
                        break;
                    case StyleAct.Pseudo:
                        break;
                    case StyleAct.Identifier:
                        element.Identifier(styleProt.Key);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            styleRule.Add(element);
        }
        
        _userInterfaceManager.Stylesheet = new Stylesheet(styleRule);
    }
    
    public MutableSelectorElement GetElement(string type)
    {
        var splited = type.Split(".");
        Logger.Debug($"ELEMENT: {splited[0]}");
        var element = new MutableSelectorElement();
        
        if (splited[0] != "*")
            element.Type = _reflectionManager.GetType(splited[0]);
        
        for (var i = 1; i < splited.Length; i++)
        {
            Logger.Debug($"CLASSIFIED: {splited[i]}");
            element.Class(splited[i]);
        }
        return element;
    }

    public void PostInitialize()
    {
        ApplySheet("default");
    }
}