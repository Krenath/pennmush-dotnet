namespace PennMUSH.Server.Commands;

using System;
using System.Text;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Server.Extensions;

public class PluginCommandHandler : ICommandHandler
{
    private readonly ExtensionManager _extensionManager;

    public string Name => "@plugin";

    public PluginCommandHandler(ExtensionManager extensionManager)
    {
        _extensionManager = extensionManager;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.IsWizard)
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (context.Switches.Contains("load"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                context.Notify("Usage: @plugin/load <assembly_path>");
                return false;
            }

            try
            {
                var record = _extensionManager.LoadExtension(context.Arguments.Trim());
                context.Notify($"Successfully loaded plugin '{record.Name}' v{record.Version} by {record.Author}.");
                return true;
            }
            catch (Exception ex)
            {
                context.Notify($"Failed to load plugin: {ex.Message}");
                return false;
            }
        }

        if (context.Switches.Contains("unload"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                context.Notify("Usage: @plugin/unload <plugin_name>");
                return false;
            }

            string name = context.Arguments.Trim();
            if (_extensionManager.UnloadExtension(name))
            {
                context.Notify($"Plugin '{name}' has been unloaded.");
                return true;
            }
            else
            {
                context.Notify($"Plugin '{name}' is not currently loaded.");
                return false;
            }
        }

        if (context.Switches.Contains("reload"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                context.Notify("Usage: @plugin/reload <plugin_name>");
                return false;
            }

            string name = context.Arguments.Trim();
            try
            {
                var record = _extensionManager.ReloadExtension(name);
                context.Notify($"Successfully reloaded plugin '{record.Name}' v{record.Version}.");
                return true;
            }
            catch (Exception ex)
            {
                context.Notify($"Failed to reload plugin: {ex.Message}");
                return false;
            }
        }

        // Default or /list: display loaded plugins table
        var plugins = _extensionManager.LoadedExtensions;
        if (plugins.Count == 0)
        {
            context.Notify("No external plugins are currently loaded.");
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== Loaded PennMUSH Extensions ===");
        sb.AppendLine(string.Format("{0,-20} {1,-10} {2,-15} {3}", "Plugin Name", "Version", "Author", "Description"));
        sb.AppendLine(new string('-', 75));

        foreach (var p in plugins)
        {
            sb.AppendLine(string.Format("{0,-20} {1,-10} {2,-15} {3}", p.Name, p.Version, p.Author, p.Description));
        }

        sb.AppendLine(new string('-', 75));
        sb.AppendLine($"Total plugins loaded: {plugins.Count}");
        context.Notify(sb.ToString().TrimEnd());
        return true;
    }
}
