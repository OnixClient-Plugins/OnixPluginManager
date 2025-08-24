using OnixRuntime.Plugin;

namespace OnixPluginManager {
    internal class PluginSources {
        public CancellationToken CancellationToken { get; }

        public InstalledPluginSource InstalledSource { get; }
        public List<RemotePluginSource> RemoteSources { get; }
        public List<PluginSourceBase> Sources { get; } = new();

        public PluginSources(CancellationToken cancellationToken) {
            CancellationToken = cancellationToken;
            InstalledSource = new InstalledPluginSource(cancellationToken, true);
            RemoteSources = PublicPluginManager.PluginInstaller.GetCuratedRemoteSourceUrls().Select(url => new RemotePluginSource(url, cancellationToken, true)).ToList();

            AddSource(InstalledSource);
            foreach (var remoteSource in RemoteSources) {
                AddSource(remoteSource);
            }
        }

        public void AddSource(PluginSourceBase source) {
            Sources.Add(source);
        }

        public List<IDisplayPlugin> GetPluginsForFrame(bool allowDuplicates = false) {
            List<IDisplayPlugin> plugins = new();
            HashSet<string> alreadyPresentUuids = new();
            foreach (var source in Sources) {
                if (source.IsEnabled) {
                    var sourcePlugins = source.GetPluginsForFrame();
                    foreach (var plugin in sourcePlugins) {
                        if (!source.IsRemoteSource) {
                            plugins.Add(GetBestVersionOf(plugin));
                            continue; // Installed plugins are always added
                        }
                        if (!allowDuplicates && alreadyPresentUuids.Contains(plugin.Manifest.Uuid)) continue;
                        alreadyPresentUuids.Add(plugin.Manifest.Uuid);
                        plugins.Add(GetBestVersionOf(plugin));
                    }
                }
            }

            return plugins;
        }

        public IDisplayPlugin? GetPluginByUuid(string uuid) {
            foreach (var source in Sources) {
                if (source.IsEnabled) {
                    var plugin = source.GetPluginByUuid(uuid);
                    if (plugin is not null) {
                        try {
                            return GetBestVersionOf(plugin);
                        } catch (Exception) {
                            // If something goes wrong, just return null
                            // this is likely there are none that are compatible
                            return plugin;
                        }
                    }
                }
            }
            return null;
        }

        public IEnumerable<IDisplayPlugin> GetAllVersionsOf(string uuid) {
            foreach (var source in Sources) {
                if (source.IsEnabled) {
                    var plugin = source.GetPluginByUuid(uuid);
                    if (plugin is not null) {
                        yield return plugin;
                    }
                }
            }
        }

        private IDisplayPlugin GetBestVersionOf(IDisplayPlugin plugin) {
            var allVersions = GetAllVersionsOf(plugin.Manifest.Uuid).ToList();
            if (allVersions.Count == 0) {
                return plugin;
            }
            try {
                var result = CompatibilityUtils.GetLatestPluginFor(allVersions);
                if (CompatibilityUtils.IsSamePlugin(result.LatestCompatiblePlugin, plugin)) {
                    plugin.UpdatedPlugins = null;
                    return plugin;
                }
                if (plugin.IsLocalVersion) {
                    plugin.UpdatedPlugins = result;
                    return plugin;
                } else if (result.LatestCompatiblePlugin?.IsLocalVersion ?? false) {
                    return plugin;
                }
                return result.LatestCompatiblePlugin ?? plugin;
            } catch (Exception) {
                // If something goes wrong, just return the original plugin
                // this is likely there are none that are compatible
                plugin.UpdatedPlugins = null;
                return plugin;
            }
        }

        public int GetDownloadCount(string uuid) {
            return GetAllVersionsOf(uuid).Aggregate(0, (count, plugin) => count + (plugin.IsLocalVersion ? 0 : plugin.DownloadCount));
        }

        public IDisplayPlugin? GetPluginByUuid(string uuid, bool isServerVersion) {
            try {
                var plugins = GetAllVersionsOf(uuid).ToList();
                if (plugins.Count == 0) {
                    return null;
                }
                if (!isServerVersion) {
                    var serverPlugin = plugins.FirstOrDefault(p => p.IsLocalVersion);
                    if (serverPlugin is not null) {
                        serverPlugin.UpdatedPlugins = CompatibilityUtils.GetLatestPluginFor(plugins);
                        return serverPlugin;
                    }
                }
                return CompatibilityUtils.GetLatestPluginFor(plugins.Where(x => !x.IsLocalVersion)).LatestCompatiblePlugin;
            } catch (Exception) {
                return null;
            }
        }
        
    }
}