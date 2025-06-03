using OnixRuntime.Api;
using OnixRuntime.Plugin;

namespace OnixPluginManager {

    internal class UpdatedDisplayPlugins {
        public IDisplayPlugin LatestCompatiblePlugin { get; }
        public IDisplayPlugin LatestIncompatiblePlugin { get; }
        public UpdatedDisplayPlugins(IDisplayPlugin latestCompatiblePlugin, IDisplayPlugin latestIncompatiblePlugin) {
            LatestCompatiblePlugin = latestCompatiblePlugin;
            LatestIncompatiblePlugin = latestIncompatiblePlugin;
        }
    }
    
    internal static class CompatibilityUtils {
        
        public static bool IsRuntimeCompatible(PluginManifest manifest) {
            return manifest.RuntimeVersion >= RuntimeManifest.Current!.MinimumRuntimeVersion && 
                   manifest.RuntimeVersion <= RuntimeManifest.Current!.RuntimeVersion;
        }
        
        public static bool IsGameCompatible(PluginManifest manifest) {
            return manifest.SupportsGameVersion(Onix.Game.Version);
        }
        
        public static bool IsCompatible(PluginManifest manifest) {
            return IsRuntimeCompatible(manifest) && 
                   IsGameCompatible(manifest);
        }

        public static List<string> GetCompatibilityError(PluginManifest manifest) {
            List<string> errors = new List<string>();
            if (manifest.RuntimeVersion < RuntimeManifest.Current!.MinimumRuntimeVersion) {
                errors.Add($"Plugin requires runtime version {manifest.RuntimeVersion}, but the lowest compatible version of this runtime is {RuntimeManifest.Current.RuntimeVersion}.");
            }
            if (manifest.RuntimeVersion > RuntimeManifest.Current.RuntimeVersion) {
                errors.Add($"Plugin requires runtime version {manifest.RuntimeVersion}, but the current runtime version is {RuntimeManifest.Current.RuntimeVersion}.");
            }
            if (!manifest.SupportsGameVersion(Onix.Game.Version)) {
                errors.Add($"Plugin does not support the current game version {Onix.Game.Version}. Supported versions: " + string.Join("\n    ", manifest.SupportedGameVersionRanges.Select(x => x.ToString())));
            }

            return errors;
        }

        public static bool IsNewerPlugin(PluginManifest a, PluginManifest b) {
            return a.PluginVersion > b.PluginVersion;
        }
        public static bool IsNewerPlugin(IDisplayPlugin a, IDisplayPlugin b) {
            return IsNewerPlugin(a.Manifest, b.Manifest);
        }

        public static bool IsSamePlugin(PluginManifest a, PluginManifest b) {
            return a.Uuid == b.Uuid &&
                   a.PluginVersion == b.PluginVersion &&
                   a.RuntimeVersion == b.RuntimeVersion &&
                   a.GameVersion == b.GameVersion;
        }
        public static bool IsSamePlugin(IDisplayPlugin a, IDisplayPlugin b) {
            return IsSamePlugin(a.Manifest, b.Manifest);
        }
        
        public static UpdatedDisplayPlugins GetLatestPluginFor(IEnumerable<IDisplayPlugin> plugins) {
            IDisplayPlugin? latestCompatible = null;
            IDisplayPlugin? latestIncompatible = null;

            foreach (var plugin in plugins) {
                if (IsRuntimeCompatible(plugin.Manifest) && IsGameCompatible(plugin.Manifest)) {
                    if (latestCompatible is null || plugin.Manifest.PluginVersion > latestCompatible.Manifest.PluginVersion) {
                        latestCompatible = plugin;
                    }
                } 
                if (latestIncompatible == null || plugin.Manifest.PluginVersion > latestIncompatible.Manifest.PluginVersion) {
                    latestIncompatible = plugin;
                }
            }

            if (latestCompatible == null || latestIncompatible == null) {
                throw new InvalidOperationException("No compatible or incompatible plugins found.");
            }

            return new UpdatedDisplayPlugins(latestCompatible, latestIncompatible);
        }
    }
}