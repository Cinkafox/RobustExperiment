using Content.Client.UserInterface.Controls;
using Content.Shared.IoC;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Client.ConfigurationUI;

[IoCRegister]
public sealed class ConfigurationUIManager : IInitializeBehavior
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;

    private readonly Dictionary<Type, IConfigurationValue> _configurationValues = new();
    private readonly Dictionary<string, ConfigurationContainer> _containers = new();
    
    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        UpdateValues();
    }

    public void RegisterConfig(ConfigurationItem item)
    {
        if(_containers.ContainsKey(item.Name)) return;
        
        _containers[item.Name] = new ConfigurationContainer()
        {
            CurrentConfigurationItem = item
        };
    }

    public ConfigurationContainer GetContained(string name)
    {
        return _containers[name];
    }

    public ConfigurationContainer GetOrCreateContainer(ConfigurationItem item)
    {
        if(_containers.TryGetValue(item.Name, out var contrainer)) return contrainer;
        
        _containers[item.Name] = new ConfigurationContainer()
        {
            CurrentConfigurationItem = item
        };

        return _containers[item.Name];
    }

    public T GetValue<T>(string name)
    {
        if (!_containers.TryGetValue(name, out var value)) 
            throw new Exception("NO VALUE");

        return (T)value.CurrentConfigurationItem.Value;
    }

    public Control GetControl(ConfigurationItem configurationItem, Action<ConfigValueChangedEvent>? onChanged)
    {
        return _configurationValues[configurationItem.Type].CreateControl(configurationItem.Value, onChanged);
    }

    private void UpdateValues()
    {
        _configurationValues.Clear();
        foreach (var type in _reflection.GetAllChildren<IConfigurationValue>())
        {
            var instance = (IConfigurationValue)_sandboxHelper.CreateInstance(type);
            _configurationValues.Add(instance.ValueType, instance);
        }
    }
}

public interface IConfigurationValue
{
    public Type ValueType { get; }
    public Control CreateControl(object value, Action<ConfigValueChangedEvent>? onChanged);
}


public sealed class BoolConfigValue : IConfigurationValue
{
    public Type ValueType => typeof(bool);

    public Control CreateControl(object value, Action<ConfigValueChangedEvent>? onChanged)
    {
        var c = new CheckBox();
        c.Pressed = (bool)value;
        c.OnToggled += COnOnToggled;

        return c;

        void COnOnToggled(BaseButton.ButtonToggledEventArgs obj)
        {
            onChanged?.Invoke(new ConfigValueChangedEvent(obj.Pressed));
        }
    }
}

public sealed class ConfigValueChangedEvent
{
    public object item;

    public ConfigValueChangedEvent(object item)
    {
        this.item = item;
    }
}
