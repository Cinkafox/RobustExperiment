using Content.Client.ConfigurationUI;

namespace Content.Client;

public static class ClientIoC
{
    public static void Register(IDependencyCollection collection)
    {
        collection.Register<ConfigurationUIManager>();
    }
}