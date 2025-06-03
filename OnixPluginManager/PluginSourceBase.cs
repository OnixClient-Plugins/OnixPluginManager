namespace OnixPluginManager {

    
    public abstract class PluginSourceBase {
        public CancellationToken CancellationToken { get; }
        public bool IsEnabled { get; set; }
        public PluginSourceBase(CancellationToken cancellationToken, bool isEnabled) {
            CancellationToken = cancellationToken;
            IsEnabled = isEnabled;
        }

        public abstract string Name { get; }
        public abstract IDisplayPlugin? GetPluginByUuid(string uuid);
        public abstract IEnumerable<IDisplayPlugin> GetPluginsForFrame();
        public abstract bool IsRemoteSource { get; }
        
    }
}