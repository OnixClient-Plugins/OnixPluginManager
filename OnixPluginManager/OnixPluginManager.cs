using OnixRuntime.Api;
using OnixRuntime.Plugin;

namespace OnixPluginManager {
    public class OnixPluginManager : OnixPluginBase {
        public static OnixPluginManager Instance { get; private set; } = null!;
        public static OnixPluginManagerConfig Config { get; private set; } = null!;
        public PluginManagerScreen ManagerScreen = null!;

        public OnixPluginManager(OnixPluginInitInfo initInfo) : base(initInfo) {
            Instance = this;
            // If you can clean up your things yourself, please don't eject the plugin when disabling.
            base.DisablingShouldUnloadPlugin = false;

#if DEBUG 
            //base.WaitForDebuggerToBeAttached();
#endif

        }

        protected override void OnLoaded() {
            Config = new OnixPluginManagerConfig(PluginDisplayModule);
            
            //Listen to events here.
            ManagerScreen = new PluginManagerScreen();
            Onix.Events.Game.UriInvokedRaw += (rawUri) => {
                if (rawUri == "onixclient://OpenPluginsManagerUi") {
                    ManagerScreen.OpenScreen();
                } else if (rawUri.StartsWith("onixclient://OpenPluginsManagerUi?OpenPlugin=")) {
                    string pluginUuid = rawUri.Substring("onixclient://OpenPluginsManagerUi?OpenPlugin=".Length);
                    ManagerScreen.OpenScreen();
                    ManagerScreen.OpenCurrentPlugin(pluginUuid, true);
                }
            };
        }

        protected override void OnEnabled() {
        }

        protected override void OnDisabled() {
            ManagerScreen.CloseScreen();
        }

        protected override void OnUnloaded() {
            ManagerScreen.CloseScreen();
        }
    }
}