using System.Diagnostics;

namespace OnixPluginManager {

    public class RemotePluginSource : PluginSourceBase {
        private readonly string Endpoint;
        private Task<List<RemoteDisplayPlugin>>? Plugins = null;
        private bool _failedToFetch;
        private Stopwatch _retryTimer = new Stopwatch();
        public RemotePluginSource(string endpoint, CancellationToken cancellationToken, bool isEnabled) : base(cancellationToken, isEnabled) { 
            Endpoint = endpoint;
        }

        private async Task<List<RemoteDisplayPlugin>> FetchPluginsAsync() {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{Endpoint}/plugins", CancellationToken);
            if (!response.IsSuccessStatusCode) {
                _failedToFetch = true;
                _retryTimer.Restart();
                return [];
            }
            _failedToFetch = false;
            var jsonText = await response.Content.ReadAsStringAsync(CancellationToken);
            try {
                var json = System.Text.Json.JsonSerializer.Deserialize<List<RemotePluginJson>>(jsonText);
                if (json is null) {
                    return [];
                }
                return json.Select(p => new RemoteDisplayPlugin(p, Endpoint)).ToList();
            } catch (Exception) {
                _failedToFetch = true;
                return [];
            }
        }


        public override string Name => $"Remote ({Endpoint})";

        public override IDisplayPlugin? GetPluginByUuid(string uuid) {
           if (Plugins?.IsCompletedSuccessfully ?? false)
               return Plugins.Result.FirstOrDefault(p => p.Manifest.Uuid == uuid);
           return null; // Not yet fetched or not in this repository
        }

        public override IEnumerable<IDisplayPlugin> GetPluginsForFrame() {
            if (_failedToFetch) {
                if (_retryTimer.Elapsed.Seconds < 10)
                    yield break; // Don't retry too quickly
                _failedToFetch = false;
                Plugins = FetchPluginsAsync();
            }
            if (Plugins is null) {
                Plugins = FetchPluginsAsync();
            }
            if (Plugins.IsCompletedSuccessfully) {
                foreach (var plugin in Plugins.Result) {
                    yield return plugin;
                }
            }
        }
        public override bool IsRemoteSource => true;
    }
}