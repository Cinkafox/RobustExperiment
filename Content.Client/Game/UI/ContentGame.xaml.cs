﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Game.UI;

[GenerateTypedNameReferences]
public sealed partial class ContentGame : UIScreen
{
    public ContentGame()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }
}