using OnixRuntime.Api.OnixClient;
using OnixRuntime.Api.Utils;
using OnixRuntime.Plugin;
using System.Net;
using System.Text.Json.Serialization;


namespace OnixPluginManager {
    public class RemoteDisplayPlugin : IDisplayPlugin {
        private string EndPoint { get; }
        DateTime LastUpdated { get; }
        public RemoteDisplayPlugin(PluginManifest manifest, int downloadCount, DateTime lastUpdated, string endPoint) {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Manifest = manifest;
            DownloadCount = downloadCount;
            LastUpdated = lastUpdated.ToLocalTime();
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        internal RemoteDisplayPlugin(RemotePluginJson json, string endPoint) : this(
            new PluginManifest(json.Manifest),
            json.DownloadCount,
            json.LastUpdated,
            endPoint) {
        }

        public PluginManifest Manifest { get; }
        public bool IsBusy => PublicPluginManager.IsPluginBusy(Manifest.Uuid);
        public bool IsInstalled => PublicPluginManager.GetPluginByUuid(Manifest.Uuid) is not null;
        public bool IsLoaded => PublicPluginManager.GetPluginByUuid(Manifest.Uuid)?.IsLoaded ?? false;
        public bool IsLocalVersion => false;
        public int DownloadCount { get; }
        public bool HasCompatibleUpdates() => false;
        public PluginState State => PublicPluginManager.GetPluginByUuid(Manifest.Uuid)?.PluginState ?? PluginState.NotInstalled;
        public OnixModule DisplayModule => throw new NotImplementedException("RemoteDisplayPlugin does not support DisplayModule directly.");

        private UpdatedDisplayPlugins? UpdatedPlugins { get; set; } = null;
        UpdatedDisplayPlugins? IDisplayPlugin.UpdatedPlugins { get => UpdatedPlugins; set => UpdatedPlugins = value; }

        DateTime IDisplayPlugin.LastUpdated => LastUpdated;
        public string DownloadUrl => $"{EndPoint}/plugins/{Manifest.Uuid}/download";

        private async Task<RawImageData?> DownloadImageAsync(string url) {
            try {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) {
                    return null;
                }
                var stream = await response.Content.ReadAsStreamAsync();
                var image = RawImageData.Load(stream);
                return image;
            } catch (Exception) {
                return null;
            }
        }

        public async Task<RawImageData> GetIconDataTask() {
            return await BannerLogoHelpers.PostProcessLogo(await DownloadImageAsync($"{EndPoint}/plugins/{Manifest.Uuid}/icon"), Manifest.Uuid);
        }
        public async Task<RawImageData> GetBannerDataTask() {
            return await BannerLogoHelpers.PostProcessBanner(await DownloadImageAsync($"{EndPoint}/plugins/{Manifest.Uuid}/banner"), Manifest.Uuid);
        }

        public string GetUniqueKey(string key) {
            string origin = IsLocalVersion ? "Local" : "Remote";
            return $"{key}_{Manifest.Uuid}_{Manifest.PluginVersion}_{origin}";
        }


        public bool Enable() {
            if (PublicPluginManager.GetPluginByUuid(Manifest.Uuid) is IOnixPlugin plugin) {
                return plugin.EnablePlugin();
            }
            return false;
        }
        public bool Disable() {
            if (PublicPluginManager.GetPluginByUuid(Manifest.Uuid) is IOnixPlugin plugin) {
                return plugin.DisablePlugin();
            }
            return false;
        }
        public void StartLoadPlugin(PluginLoadMode mode) { 
            if (PublicPluginManager.GetPluginByUuid(Manifest.Uuid) is IOnixPlugin plugin) {
                plugin.StartLoadPlugin(mode);
            }
        }
        public void StartUnloadPlugin() { }

    }


    internal class RemotePluginJson {
        [JsonPropertyName("manifest")]
        public required PluginManifestJson Manifest { get; set; }
        [JsonPropertyName("download_count")]
        public required int DownloadCount { get; set; }
        [JsonPropertyName("last_updated")]
        public required DateTime LastUpdated { get; set; }
    }
}