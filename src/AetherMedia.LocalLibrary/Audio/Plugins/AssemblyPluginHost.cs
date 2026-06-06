// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Runtime.Loader;

namespace AetherMedia.LocalLibrary.Audio.Plugins;

/// <summary>
/// Default <see cref="IPluginHost"/> that discovers plugins by scanning a
/// directory for managed assemblies and instantiating every type that
/// implements one of the plugin contracts.
///
/// <para>
/// Each plugin folder is loaded into its own <see cref="AssemblyLoadContext"/>
/// so unloading + side-by-side versions are possible later. Type
/// instantiation fails are caught and logged via the supplied callback —
/// one bad plugin doesn't take the host down.
/// </para>
/// </summary>
public sealed class AssemblyPluginHost : IPluginHost
{
    private readonly string _pluginsDirectory;
    private readonly Action<Exception, string>? _onLoadError;
    private readonly List<IInputPlugin>          _input   = new();
    private readonly List<IOutputPlugin>         _output  = new();
    private readonly List<IGeneralPurposePlugin> _general = new();
    private readonly List<ILibraryPlugin>        _library = new();

    public AssemblyPluginHost(string pluginsDirectory, Action<Exception, string>? onLoadError = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(pluginsDirectory);
        _pluginsDirectory = pluginsDirectory;
        _onLoadError = onLoadError;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IInputPlugin>          InputPlugins   => _input;
    public IReadOnlyList<IOutputPlugin>         OutputPlugins  => _output;
    public IReadOnlyList<IGeneralPurposePlugin> GeneralPlugins => _general;
    public IReadOnlyList<ILibraryPlugin>        LibraryPlugins => _library;

    /// <inheritdoc/>
    public Task LoadAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_pluginsDirectory)) return Task.CompletedTask;

        foreach (var dll in Directory.EnumerateFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            Assembly? asm = null;
            try
            {
                var alc = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dll), isCollectible: true);
                asm = alc.LoadFromAssemblyPath(dll);
            }
            catch (BadImageFormatException) { continue; }
            catch (FileLoadException ex) { _onLoadError?.Invoke(ex, dll); continue; }

            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance is null) continue;

                    if (instance is IInputPlugin ip          && !Has(_input,   ip.Id))       _input.Add(ip);
                    if (instance is IOutputPlugin op         && !Has(_output,  op.Id))       _output.Add(op);
                    if (instance is IGeneralPurposePlugin gp && !Has(_general, gp.Id))       _general.Add(gp);
                    if (instance is ILibraryPlugin lp        && !Has(_library, lp.Id))       _library.Add(lp);
                }
                catch (Exception ex) when (ex is MissingMethodException or TargetInvocationException)
                {
                    _onLoadError?.Invoke(ex, $"{dll}::{type.FullName}");
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool Has<T>(IReadOnlyList<T> list, string id) where T : class
    {
        foreach (var item in list)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop?.GetValue(item) is string existing && string.Equals(existing, id, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
