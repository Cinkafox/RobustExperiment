using Content.Client.ConfigurationUI;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class ConfigurationContainer : BoxContainer
{
    [Dependency] private readonly ConfigurationUIManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    
    private ConfigurationItem? _configurationItem;

    public ConfigurationItem CurrentConfigurationItem
    {
        get
        {
            if (_configurationItem is null) throw new Exception();
            return _configurationItem;
        }

        set
        {
            _configurationItem = value;
            UpdateControl();
        }
    }

    public ConfigurationContainer()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Horizontal;
        Margin = new Thickness(0,5,0,5);
        HorizontalExpand = true;
    }

    private void UpdateControl()
    {
        Children.Clear();

        var label = new Label()
        {
            Text = CurrentConfigurationItem.Name,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Left,
            Margin = new Thickness(0,0,5,0)
        };
        Children.Add(label);

        var valueContainer = _configuration.GetControl(CurrentConfigurationItem.Type, OnChanged);
        valueContainer.HorizontalExpand = true;
        valueContainer.HorizontalAlignment = HAlignment.Right;
        Children.Add(valueContainer);
    }

    private void OnChanged(ConfigValueChangedEvent obj)
    {
        CurrentConfigurationItem.Value = obj.item;
    }
}

public class ConfigurationItem
{
    public string Name { get; }
    public Type Type { get; }

    public object? Value { get; set; } = null;
    
    public ConfigurationItem(string name, Type type)
    {
        Name = name;
        Type = type;
    }
}

public sealed class ConfigurationItem<T> : ConfigurationItem
{
    public ConfigurationItem(string name) : base(name, typeof(T))
    {
    }
}
