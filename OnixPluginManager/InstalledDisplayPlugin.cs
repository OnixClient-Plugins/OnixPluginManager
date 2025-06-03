using OnixRuntime.Api.OnixClient;
using OnixRuntime.Api.Utils;
using OnixRuntime.Plugin;

namespace OnixPluginManager {
    internal class InstalledDisplayPlugin : IDisplayPlugin {
        private IOnixPlugin _plugin;

        public InstalledDisplayPlugin(IOnixPlugin plugin) {
            _plugin = plugin;
        }

        public PluginManifest Manifest => _plugin.Manifest;
        public bool IsBusy => _plugin.IsBusy;
        public bool IsInstalled => true;
        public bool IsLoaded => _plugin.IsLoaded;
        public bool IsLocalVersion => true;
        public int DownloadCount => IsInstalled ? 1 : 0;
        private DateTime? _lastUpdated = null;
        public DateTime LastUpdated {
            get {
                if (_lastUpdated is null) {
                    try {
                        _lastUpdated = File.GetLastWriteTime(Path.Combine(_plugin.PluginFolder, _plugin.Manifest.TargetAssemblyName));
                    } catch (Exception) {
                        _lastUpdated = new DateTime(1984, 9, 21, 12, 0, 54);
                    }
                }
                return _lastUpdated.Value;
            }
        }
        public string DownloadUrl => string.Empty; // Local plugins do not have a download URL.
        
        private UpdatedDisplayPlugins? UpdatedPlugins { get; set; } = null;
        public PluginState State => _plugin.PluginState;
        public OnixModule DisplayModule => _plugin.DisplayModule;

        UpdatedDisplayPlugins? IDisplayPlugin.UpdatedPlugins { get => UpdatedPlugins; set => UpdatedPlugins = value; }

        public bool HasCompatibleUpdates() {
            if (UpdatedPlugins is null) {
                return false;
            }
            return CompatibilityUtils.IsSamePlugin(UpdatedPlugins.LatestCompatiblePlugin, this) && !UpdatedPlugins.LatestCompatiblePlugin.IsLocalVersion;
        }

        public async Task<RawImageData> GetIconDataTask() {
            string path = Path.Combine(_plugin.AssetsDirectory, "PluginIcon.png");
            RawImageData? image = null;
            if (File.Exists(path)) {
                try {
                    image = RawImageData.Load(path);
                } catch (Exception) { }
            }

            return await BannerLogoHelpers.PostProcessLogo(image, Manifest.Uuid + "logo");
        }
        public async Task<RawImageData> GetBannerDataTask() {
            string path = Path.Combine(_plugin.AssetsDirectory, "PluginBanner.png");
            RawImageData? image = null;
            if (File.Exists(path)) {
                try {
                    image = RawImageData.Load(path);
                } catch (Exception) {
                }
            }

            return await BannerLogoHelpers.PostProcessBanner(image, Manifest.Uuid + "banner");
        }
        public string GetUniqueKey(string key) {
            string origin = IsLocalVersion ? "Local" : "Remote";
            return $"{key}_{Manifest.Uuid}_{Manifest.PluginVersion}_{origin}";
        }

        
        public bool Enable() {
            return _plugin.EnablePlugin();
        }
        public bool Disable() {
            return _plugin.DisablePlugin();
        }

        public void StartLoadPlugin(PluginLoadMode mode) {
            _plugin.StartLoadPlugin(mode);
        }

        public void StartUnloadPlugin() {
            _plugin.StartUnloadPlugin();
        }
    }
}