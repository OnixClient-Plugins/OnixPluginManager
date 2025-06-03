using OnixRuntime.Api.OnixClient;
using OnixRuntime.Api.Utils;
using OnixRuntime.Plugin;

namespace OnixPluginManager {
    
    public interface IDisplayPlugin {
        public PluginManifest Manifest { get; }
        public bool IsBusy { get; }
        public bool IsInstalled { get; }
        public bool IsLoaded { get; }
        public bool IsLocalVersion { get; }
        public int DownloadCount { get; }
        public PluginState State { get; }
        public OnixModule DisplayModule { get; }

        internal UpdatedDisplayPlugins? UpdatedPlugins {
            get;
            set;
        }

        DateTime LastUpdated { get; }
        string DownloadUrl { get; }

        public Task<RawImageData> GetIconDataTask();
        public Task<RawImageData> GetBannerDataTask();
        public string GetUniqueKey(string key);

        bool Enable();
        bool Disable();
        void StartLoadPlugin(PluginLoadMode loadMode);
        void StartUnloadPlugin(bool oneOff);
        
    }
}