namespace PennMUSH.Server.Extensions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;
using PennMUSH.Extensions;
using PennMUSH.Server.Engine;

public record ExtensionRecord(
    string Name,
    string Version,
    string Description,
    string Author,
    string FilePath,
    IExtension Instance,
    ExtensionLoadContext Context,
    ExtensionHostProxy Host);

public class ExtensionManager : IDisposable
{
    private readonly MUSHServer _server;
    private readonly ConcurrentDictionary<string, ExtensionRecord> _extensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IProtocolHandler> _protocolHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (ITickService Service, CancellationTokenSource Cts, Task Task)> _tickServices = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ExtensionRecord> LoadedExtensions => _extensions.Values.ToList().AsReadOnly();
    public bool IsLoaded(string name) => _extensions.ContainsKey(name);

    public ExtensionManager(MUSHServer server)
    {
        _server = server;
    }

    public void ScanAndLoadPlugins(params string[] searchPaths)
    {
        var visitedDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in searchPaths)
        {
            if (!Directory.Exists(dir)) continue;

            var dlls = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dlls)
            {
                // Ignore core server assemblies
                string fileName = Path.GetFileName(dll);
                if (fileName.StartsWith("PennMUSH.Core", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("PennMUSH.Server", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("PennMUSH.Extensions", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string full = Path.GetFullPath(dll);
                if (!visitedDlls.Add(full)) continue;

                try
                {
                    var record = LoadExtension(full);
                    Console.WriteLine($"[ExtensionManager] Loaded extension '{record.Name}' v{record.Version} from {full}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExtensionManager] Notice: Could not load plugin '{dll}': {ex.Message}");
                }
            }

            // Enable file system watcher for live hot-reloading
            WatchPluginDirectory(dir);
        }
    }

    public ExtensionRecord LoadExtension(string dllPath)
    {
        string fullPath = ResolveDllPath(dllPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found at: {fullPath}");
        }

        var context = new ExtensionLoadContext(fullPath);
        Assembly assembly;
        try
        {
            assembly = context.LoadAssemblyWithoutLock(fullPath);
        }
        catch
        {
            context.Unload();
            throw;
        }

        var extensionType = assembly.GetTypes().FirstOrDefault(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        if (extensionType == null)
        {
            context.Unload();
            throw new InvalidOperationException($"No class implementing IExtension found in {fullPath}");
        }

        var instance = (IExtension)Activator.CreateInstance(extensionType)!;
        if (_extensions.ContainsKey(instance.Name))
        {
            UnloadExtension(instance.Name);
        }

        var hostProxy = new ExtensionHostProxy(this, _server, instance.Name);
        try
        {
            instance.Initialize(hostProxy);
        }
        catch
        {
            hostProxy.Cleanup();
            context.Unload();
            throw;
        }

        var record = new ExtensionRecord(
            instance.Name,
            instance.Version,
            instance.Description,
            instance.Author,
            fullPath,
            instance,
            context,
            hostProxy);

        _extensions[instance.Name] = record;
        return record;
    }

    private static string ResolveDllPath(string path)
    {
        if (File.Exists(path)) return Path.GetFullPath(path);

        string fileName = Path.GetFileName(path);
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), path),
            Path.Combine(Directory.GetCurrentDirectory(), "plugins", fileName),
            Path.Combine(AppContext.BaseDirectory, path),
            Path.Combine(AppContext.BaseDirectory, "plugins", fileName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "plugins", fileName)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "plugins", fileName)),
            Path.Combine(@"C:\Repos\pennmush-dotnet\plugins", fileName)
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        return Path.GetFullPath(path);
    }

    public bool UnloadExtension(string name)
    {
        if (!_extensions.TryRemove(name, out var record))
        {
            return false;
        }

        try
        {
            record.Instance.Shutdown();
        }
        catch
        {
            // Suppress shutdown exceptions to ensure cleanup continues
        }

        record.Host.Cleanup();
        record.Context.Unload();
        return true;
    }

    public ExtensionRecord ReloadExtension(string name)
    {
        if (!_extensions.TryGetValue(name, out var record))
        {
            throw new KeyNotFoundException($"Extension '{name}' is not loaded.");
        }

        string path = record.FilePath;
        UnloadExtension(name);

        // Force GC collection to release memory before reload
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return LoadExtension(path);
    }

    internal void RegisterProtocolHandlerInternal(string prefix, IProtocolHandler handler)
    {
        _protocolHandlers[prefix] = handler;
    }

    internal void UnregisterProtocolHandlerInternal(string prefix)
    {
        _protocolHandlers.TryRemove(prefix, out _);
    }

    internal void RegisterTickServiceInternal(ITickService service)
    {
        UnregisterTickServiceInternal(service.Name);

        var cts = new CancellationTokenSource();
        var task = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(service.Interval, cts.Token);
                    await service.ExecuteTickAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Suppress individual tick errors
                }
            }
        }, cts.Token);

        _tickServices[service.Name] = (service, cts, task);
    }

    internal void UnregisterTickServiceInternal(string name)
    {
        if (_tickServices.TryRemove(name, out var entry))
        {
            try
            {
                entry.Cts.Cancel();
                entry.Cts.Dispose();
            }
            catch
            {
                // Ignored
            }
        }
    }

    public async Task<bool> TryDispatchProtocolEnvelopeAsync(Dbref player, string type, JsonElement payload)
    {
        foreach (var (prefix, handler) in _protocolHandlers)
        {
            if (type.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var record = _extensions.Values.FirstOrDefault();
                var host = record?.Host ?? new ExtensionHostProxy(this, _server, "Default");
                await handler.HandleEnvelopeAsync(player, type, payload, host);
                return true;
            }
        }

        return false;
    }

    private void WatchPluginDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return;

        try
        {
            var watcher = new FileSystemWatcher(dir, "*.dll")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            watcher.Changed += OnPluginFileChanged;
            watcher.Created += OnPluginFileChanged;
            _watchers.Add(watcher);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExtensionManager] Notice: Could not watch plugin dir '{dir}': {ex.Message}");
        }
    }

    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        string fileName = Path.GetFileName(e.FullPath);
        if (fileName.StartsWith("PennMUSH.Core", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("PennMUSH.Server", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("PennMUSH.Extensions", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Debounce by 500ms so compile/copy finishes writing before reload
        _debounceTimers.AddOrUpdate(
            e.FullPath,
            path => new Timer(_ => TriggerHotReload(path), null, 500, Timeout.Infinite),
            (path, oldTimer) =>
            {
                oldTimer.Dispose();
                return new Timer(_ => TriggerHotReload(path), null, 500, Timeout.Infinite);
            });
    }

    private void TriggerHotReload(string fullPath)
    {
        _debounceTimers.TryRemove(fullPath, out var timer);
        timer?.Dispose();

        // Match against existing loaded extension
        var existing = _extensions.Values.FirstOrDefault(r =>
            string.Equals(Path.GetFileName(r.FilePath), Path.GetFileName(fullPath), StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            try
            {
                Console.WriteLine($"[ExtensionManager] Detected change in '{existing.Name}'. Hot-reloading live...");
                var reloaded = ReloadExtension(existing.Name);
                Console.WriteLine($"[ExtensionManager] Successfully hot-reloaded '{reloaded.Name}' v{reloaded.Version} with 0 disconnects.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtensionManager] Hot-reload error for '{existing.Name}': {ex.Message}");
            }
        }
        else
        {
            try
            {
                var record = LoadExtension(fullPath);
                Console.WriteLine($"[ExtensionManager] Auto-loaded new extension '{record.Name}' v{record.Version}.");
            }
            catch
            {
                // Ignored if file wasn't an IExtension
            }
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();

        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }
        _debounceTimers.Clear();

        foreach (var name in _extensions.Keys.ToList())
        {
            UnloadExtension(name);
        }
    }
}
