namespace PennMUSH.Server.Extensions;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

public class ExtensionLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDirectory;

    public ExtensionLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginDirectory = Path.GetDirectoryName(Path.GetFullPath(pluginPath)) ?? AppContext.BaseDirectory;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try shared host assemblies first (PennMUSH.Core, PennMUSH.Extensions, etc.)
        try
        {
            var defaultAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            if (defaultAssembly != null)
            {
                return defaultAssembly;
            }
        }
        catch
        {
            // Fall through to local plugin directory
        }

        string candidatePath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(candidatePath))
        {
            return LoadFromAssemblyPath(candidatePath);
        }

        return null;
    }
}
