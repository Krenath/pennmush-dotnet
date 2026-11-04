namespace PennMUSH.Extensions;

public interface IExtension
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }

    void Initialize(IExtensionHost host);
    void Shutdown();
}
