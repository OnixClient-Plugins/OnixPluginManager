using OnixRuntime.Plugin;

namespace OnixPluginManager {

    internal class InstalledPluginSource : PluginSourceBase {
        public InstalledPluginSource(CancellationToken cancellationToken, bool isEnabled) : base(cancellationToken, isEnabled) { }

        public override IDisplayPlugin? GetPluginByUuid(string uuid) {
            var plugin = PublicPluginManager.GetPluginByUuid(uuid);
            if (plugin is null) {
                return null;
            }
            return new InstalledDisplayPlugin(plugin);
        }

        public override IEnumerable<IDisplayPlugin> GetPluginsForFrame() {
            foreach (var plugin in PublicPluginManager.GetPlugins()) {
                yield return new InstalledDisplayPlugin(plugin);
            }
        }

        public override bool IsRemoteSource => false;
        public override string Name => "Installed";
        public override bool CanRemove => false;
    }
}