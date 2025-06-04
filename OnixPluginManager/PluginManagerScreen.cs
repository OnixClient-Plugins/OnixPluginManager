using OnixRuntime.Api;
using OnixRuntime.Api.Inputs;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.OnixClient;
using OnixRuntime.Api.Rendering;
using OnixRuntime.Api.Rendering.Helpers;
using OnixRuntime.Api.Utils;
using OnixRuntime.Plugin;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata;

namespace OnixPluginManager {

    public class PluginManagerScreen : OnixClientScreen {
        private CancellationTokenSource _cancellationTokenSource;
        private PluginSources Sources;
        private Dictionary<string, Task<RawImageData>> _logoDataTasks = new();
        private Dictionary<string, Task<RawImageData>> _bannerDataTasks = new();
        private Stopwatch _screenRuntimeTracker = Stopwatch.StartNew();
        private Stopwatch _deltaTimeTracker = Stopwatch.StartNew();
        private float _deltaTime = 0f;
        private ScrollbarPanelLogic _scrollbarPanelLogic;
        private OnixTextbox _pluginSearchBox;
        private MainTabs _currentTab = MainTabs.Installed;
        private PluginRelevances _pluginRelevance = PluginRelevances.AToZ;
        private PluginRelevances _pluginRelevanceDiscover = PluginRelevances.Popular;
        private float _lastKnownPlayerYaw = 45f;
        private int _lastKnownUpdateCount = 0;
        private string? _currentlyOpenedPluginUuid = null;
        private bool _currentlyOpenedPluginIsServer = false;
        private bool _currentlyOpenedPluginIsNew = false;

        public OnixSettingListRenderer _settingListRenderer = new();

        private SvgRenderer _downloadCountIcon = SvgRenderer.Create("DownloadCountIcon.svg", @"<svg width=""21"" height=""20"" viewBox=""0 0 21 20"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M2.00037 15.833V12.5C2.00037 12.0398 2.37314 11.667 2.83337 11.667C3.29361 11.667 3.66638 12.0398 3.66638 12.5V15.833L3.67126 15.916C3.69028 16.1066 3.77386 16.2862 3.91052 16.4229C4.0668 16.5791 4.27935 16.667 4.50037 16.667H16.1664C16.3874 16.667 16.5999 16.5791 16.7562 16.4229C16.9125 16.2666 17.0004 16.054 17.0004 15.833V12.5C17.0004 12.0398 17.3731 11.667 17.8334 11.667C18.2936 11.667 18.6664 12.0398 18.6664 12.5V15.833C18.6664 16.496 18.4038 17.1327 17.9349 17.6016C17.4661 18.0704 16.8294 18.333 16.1664 18.333H4.50037C3.83732 18.333 3.20065 18.0704 2.73181 17.6016C2.32149 17.1912 2.06884 16.6525 2.01208 16.0801L2.00037 15.833Z"" fill=""white"" fill-opacity=""1""/>
<path fill-rule=""evenodd"" clip-rule=""evenodd"" d=""M9.75017 13.0949L9.74386 13.0892L5.57784 8.9222L5.52022 8.8597C5.25307 8.53238 5.27267 8.04963 5.57784 7.74447C5.88301 7.4393 6.36576 7.41969 6.69307 7.68685L6.75557 7.74447L9.50037 10.4885V12.5C9.50037 12.7331 9.59599 12.9438 9.75017 13.0949ZM9.75017 13.0949L9.79786 13.1382C9.78141 13.1244 9.7655 13.11 9.75017 13.0949ZM11.1664 10.4874L10.3327 11.3206L9.50037 10.4885V2.5C9.50037 2.03976 9.87314 1.66699 10.3334 1.66699C10.7936 1.66699 11.1664 2.03976 11.1664 2.5V10.4874ZM11.1664 10.4874V12.5C11.1664 12.9602 10.7936 13.333 10.3334 13.333C10.1294 13.333 9.94263 13.2598 9.79786 13.1382L9.80733 13.1468C10.1346 13.4137 10.6175 13.3943 10.9226 13.0892L15.0896 8.9222C15.4146 8.59685 15.4146 8.06982 15.0896 7.74447C14.7845 7.43942 14.3016 7.42003 13.9743 7.68685L13.9108 7.74447L11.1664 10.4874Z"" fill=""white"" fill-opacity=""1""/>
<path d=""M10.3334 13.333C10.7936 13.333 11.1664 12.9602 11.1664 12.5V10.4874L10.3327 11.3206L9.50037 10.4885V12.5C9.50037 12.7331 9.59599 12.9438 9.75017 13.0949L9.79786 13.1382C9.94263 13.2598 10.1294 13.333 10.3334 13.333Z"" fill=""white"" fill-opacity=""1""/>
</svg>");
        private SvgRenderer _alreadyInstalledIcon = SvgRenderer.Create("AlreadyInstalledIcon.svg", (string)@"<svg width=""18"" height=""18"" viewBox=""0 0 18 18"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M14.3333 5L8.16667 13.1667L6.08333 11.0833L4 9"" stroke=""white"" stroke-width=""1.66667"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _updateTimeIcon = SvgRenderer.Create("UpdateTimeIcon.svg", @"<svg width=""20"" height=""20"" viewBox=""0 0 20 20"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M14.0844 9.17188C13.6668 9.21703 13.342 9.57042 13.342 10C13.342 10.4296 13.6668 10.783 14.0843 10.8282C14.1383 10.5604 14.1667 10.2834 14.1667 9.99969C14.1667 9.71625 14.1383 9.43942 14.0844 9.17188Z"" fill=""white""/>
<path fill-rule=""evenodd"" clip-rule=""evenodd"" d=""M12.4997 9.99969C12.4995 8.61913 11.3803 7.49969 9.99968 7.49969C8.61923 7.49987 7.49986 8.61924 7.49968 9.99969C7.49968 11.3803 8.61912 12.4995 9.99968 12.4997C11.3804 12.4997 12.4997 11.3804 12.4997 9.99969ZM9.99968 14.1667C12.0172 14.1667 13.7001 12.7325 14.0843 10.8282C14.1141 10.8314 14.1444 10.833 14.175 10.833H19.133L19.219 10.8291C19.639 10.7863 19.967 10.4314 19.967 10C19.967 9.56868 19.639 9.21378 19.219 9.17092L19.133 9.16701H14.175C14.1444 9.16701 14.1141 9.16866 14.0844 9.17188C13.7002 7.2675 12.0173 5.83368 9.99968 5.83368C7.98259 5.83383 6.30057 7.26705 5.91617 9.17079L5.83301 9.16701H0.875C0.414763 9.16701 0.0419922 9.53978 0.0419922 10C0.0419922 10.4603 0.414763 10.833 0.875 10.833H5.83301L5.91623 10.8292C6.30074 12.733 7.98269 14.1665 9.99968 14.1667ZM5.91623 10.8292L5.91895 10.8291C6.33897 10.7863 6.66699 10.4314 6.66699 10C6.66699 9.56868 6.33897 9.21378 5.91895 9.17092L5.91617 9.17079C5.86209 9.43867 5.83369 9.71587 5.83367 9.99969C5.83367 10.2837 5.86209 10.5612 5.91623 10.8292ZM14.0844 9.17188C14.1383 9.43942 14.1667 9.71625 14.1667 9.99969C14.1667 10.2834 14.1383 10.5604 14.0843 10.8282C13.6668 10.783 13.342 10.4296 13.342 10C13.342 9.57042 13.6668 9.21703 14.0844 9.17188Z"" fill=""white""/>
<path d=""M6.66699 10C6.66699 9.56868 6.33897 9.21378 5.91895 9.17092L5.91617 9.17079C5.86209 9.43867 5.83369 9.71587 5.83367 9.99969C5.83367 10.2837 5.86209 10.5612 5.91623 10.8292L5.91895 10.8291C6.33897 10.7863 6.66699 10.4314 6.66699 10Z"" fill=""white""/>
</svg>");
        private SvgRenderer _versionIcon = SvgRenderer.Create("VersionIcon.svg", @"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M16.5 9.40001L7.5 4.21001"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M21 16V7.99999C20.9996 7.64927 20.9071 7.3048 20.7315 7.00116C20.556 6.69751 20.3037 6.44536 20 6.26999L13 2.26999C12.696 2.09446 12.3511 2.00204 12 2.00204C11.6489 2.00204 11.304 2.09446 11 2.26999L4 6.26999C3.69626 6.44536 3.44398 6.69751 3.26846 7.00116C3.09294 7.3048 3.00036 7.64927 3 7.99999V16C3.00036 16.3507 3.09294 16.6952 3.26846 16.9988C3.44398 17.3025 3.69626 17.5546 4 17.73L11 21.73C11.304 21.9055 11.6489 21.9979 12 21.9979C12.3511 21.9979 12.696 21.9055 13 21.73L20 17.73C20.3037 17.5546 20.556 17.3025 20.7315 16.9988C20.9071 16.6952 20.9996 16.3507 21 16Z"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M3.27002 6.96001L12 12.01L20.73 6.96001"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M12 22.08V12"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _pluginSearchIcon = SvgRenderer.Create("SearchIcon.svg", (string)@"<svg width=""20"" height=""20"" viewBox=""0 0 20 20"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path fill-rule=""evenodd"" clip-rule=""evenodd"" d=""M9 2C12.866 2 16 5.13401 16 9C16 10.8862 15.254 12.5982 14.0409 13.8569C14.0069 13.8831 13.9741 13.9118 13.943 13.943C13.9118 13.9741 13.8831 14.0069 13.8569 14.0409C12.5982 15.254 10.8862 16 9 16C5.13401 16 2 12.866 2 9C2 5.13401 5.13401 2 9 2ZM14.6178 16.0318L13.943 15.357L13.8746 15.2809C13.5815 14.9215 13.5755 14.4063 13.8569 14.0409C13.9194 13.9807 13.9807 13.9194 14.0409 13.8569C14.4063 13.5755 14.9215 13.5815 15.2809 13.8746L15.357 13.943L16.0318 14.6178C15.6143 15.1398 15.1398 15.6143 14.6178 16.0318ZM14.6178 16.0318L18.2926 19.7066L18.3687 19.776C18.7615 20.0963 19.3405 20.0727 19.7066 19.7066C20.0727 19.3405 20.0963 18.7615 19.776 18.3687L19.7066 18.2926L16.0318 14.6178C17.2635 13.0781 18 11.1251 18 9C18 4.02944 13.9706 0 9 0C4.02944 0 0 4.02944 0 9C0 13.9706 4.02944 18 9 18C11.1251 18 13.0781 17.2635 14.6178 16.0318Z"" fill=""white""/>
<path d=""M15.357 13.943L15.2809 13.8746C14.9215 13.5815 14.4063 13.5755 14.0409 13.8569C13.9807 13.9194 13.9194 13.9807 13.8569 14.0409C13.5755 14.4063 13.5815 14.9215 13.8746 15.2809L13.943 15.357L14.6178 16.0318C15.1398 15.6143 15.6143 15.1398 16.0318 14.6178L15.357 13.943Z"" fill=""white""/>
</svg>");
        private SvgRenderer _relevanceIcon = SvgRenderer.Create("RelevanceIcon.svg", (string)@"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M4 12H14"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M4 6L20 6"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M4 18H8"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _trashcanIcon = SvgRenderer.Create("RelevanceIcon.svg", (string)@"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M6 12H10M10 12H42M10 12V40C10 41.0609 10.4214 42.0783 11.1716 42.8284C11.9217 43.5786 12.9391 44 14 44H34C35.0609 44 36.0783 43.5786 36.8284 42.8284C37.5786 42.0783 38 41.0609 38 40V12M16 12V8C16 6.93913 16.4214 5.92172 17.1716 5.17157C17.9217 4.42143 18.9391 4 20 4H28C29.0609 4 30.0783 4.42143 30.8284 5.17157C31.5786 5.92172 32 6.93913 32 8V12M20 22V34M28 22V34"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");

        private SvgRenderer _filtersIcon = SvgRenderer.Create("FiltersIcon.svg", (string)@"<svg width=""22"" height=""20"" viewBox=""0 0 22 20"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M21 1H1L9 10.46V17L13 19V10.46L21 1Z"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _downArrowIcon = SvgRenderer.Create("DownArrowIcon.svg", (string)@"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M6 9L12 15L18 9"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _popularRelevancyIcon = SvgRenderer.Create("PopularRelevancyIcon.svg", (string)@"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M46 12L27 31L17 21L2 36M46 12H34M46 12V24"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _newRelevancyIcon = SvgRenderer.Create("NewRelevancyIcon.svg", (string)@"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M24 4L30.18 16.52L44 18.54L34 28.28L36.36 42.04L24 35.54L11.64 42.04L14 28.28L4 18.54L17.82 16.52L24 4Z"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _aToZRelevancyIcon = SvgRenderer.Create("AToZRelevancyIcon.svg", (string)@"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M10 24H38M38 24L24 10M38 24L24 38"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _zToARelevancyIcon = SvgRenderer.Create("ZToARelevancyIcon.svg", (string)@"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M38 24H10M10 24L24 38M10 24L24 10"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _updateIcon = SvgRenderer.Create("UpdateIcon.svg", @"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<g clip-path=""url(#clip0_18_80)"">
<path d=""M20.25 6V10.5M20.25 10.5H15.75M20.25 10.5L16.77 7.23C15.9639 6.42353 14.9667 5.8344 13.8714 5.51758C12.7761 5.20075 11.6183 5.16656 10.5062 5.41819C9.3941 5.66982 8.36385 6.19907 7.5116 6.95656C6.65935 7.71405 6.01288 8.67508 5.6325 9.75M3.75 18V13.5M3.75 13.5H8.25M3.75 13.5L7.23 16.77C8.03606 17.5765 9.03328 18.1656 10.1286 18.4824C11.2239 18.7992 12.3817 18.8334 13.4938 18.5818C14.6059 18.3302 15.6361 17.8009 16.4884 17.0434C17.3407 16.2859 17.9871 15.3249 18.3675 14.25"" stroke=""white"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</g>
<defs>
<clipPath id=""clip0_18_80"">
<rect width=""18"" height=""18"" fill=""white"" transform=""translate(3 3)""/>
</clipPath>
</defs>
</svg>");
        private SvgRenderer _enableIcon = SvgRenderer.Create("EnableIcon.svg", @"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M32 10H16C8.26801 10 2 16.268 2 24C2 31.732 8.26801 38 16 38H32C39.732 38 46 31.732 46 24C46 16.268 39.732 10 32 10Z"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M32 30C35.3137 30 38 27.3137 38 24C38 20.6863 35.3137 18 32 18C28.6863 18 26 20.6863 26 24C26 27.3137 28.6863 30 32 30Z"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _disableIcon = SvgRenderer.Create("DisableIcon.svg", @"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M32 10H16C8.26801 10 2 16.268 2 24C2 31.732 8.26801 38 16 38H32C39.732 38 46 31.732 46 24C46 16.268 39.732 10 32 10Z"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M16 30C19.3137 30 22 27.3137 22 24C22 20.6863 19.3137 18 16 18C12.6863 18 10 20.6863 10 24C10 27.3137 12.6863 30 16 30Z"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _loadIcon = SvgRenderer.Create("LoadIcon.svg", @"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M46 8V20M46 20H34M46 20L36.74 11.28C33.9812 8.51947 30.4 6.73037 26.5359 6.18228C22.6719 5.6342 18.7343 6.35683 15.3167 8.24128C11.8991 10.1257 9.1865 13.0699 7.58773 16.6301C5.98896 20.1904 5.59062 24.1738 6.45273 27.9801C7.31485 31.7864 9.39071 35.2094 12.3675 37.7333C15.3443 40.2572 19.0608 41.7452 22.9568 41.9732C26.8529 42.2011 30.7175 41.1566 33.9683 38.997C37.2191 36.8374 39.6799 33.6798 40.98 30"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _unloadIcon = SvgRenderer.Create("UnloadIcon.svg", @"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M2 8.00001V20M2 20H14M2 20L11.28 11.28C14.0418 8.52285 17.6249 6.73794 21.4894 6.19424C25.3538 5.65053 29.2903 6.37747 32.7057 8.26553C36.1211 10.1536 38.8303 13.1005 40.4252 16.6622C42.0202 20.2239 42.4144 24.2075 41.5484 28.0127C40.6825 31.818 38.6034 35.2387 35.6243 37.7595C32.6452 40.2803 28.9275 41.7647 25.0314 41.9889C21.1354 42.2132 17.272 41.1651 14.0233 39.0028C10.7747 36.8404 8.31679 33.6808 7.02 30"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");
        private SvgRenderer _onlineViewIcon = SvgRenderer.Create("OnlineViewIcon.svg", @"<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M24 32V24M24 16H24.02M44 24C44 35.0457 35.0457 44 24 44C12.9543 44 4 35.0457 4 24C4 12.9543 12.9543 4 24 4C35.0457 4 44 12.9543 44 24Z"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>");

        public enum MainTabs {
            Back,
            Installed,
            Discover,
            Updates
        }
        public enum SelectedPluginTabs {
            Back,
            LoadUnload,
            EnableDisable,
            InstallUninstall,
            PackagePlugin,
            Update,
            OnlineView,
        }


        public enum PluginRelevances {
            Popular,
            New,
            AToZ,
            ZToA,
        }

        private SvgRenderer[] TabIcons = [
            SvgRenderer.Create("TabBack.svg", @"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M16 4L8 12L16 20"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round""/>
</svg>"),
            SvgRenderer.Create("TabInstalled.svg", @"<svg width=""18"" height=""18"" viewBox=""0 0 18 18"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M14.3333 5L8.16667 13.1667L6.08333 11.0833L4 9"" stroke=""white"" stroke-width=""1.66667"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>"),
            SvgRenderer.Create("TabDiscover.svg", @"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<path d=""M12 22C17.5228 22 22 17.5228 22 12C22 6.47715 17.5228 2 12 2C6.47715 2 2 6.47715 2 12C2 17.5228 6.47715 22 12 22Z"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
<path d=""M16.24 7.75999L14.12 14.12L7.76001 16.24L9.88001 9.87999L16.24 7.75999Z"" stroke=""white"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</svg>"),
            SvgRenderer.Create("TabUpdates.svg", @"<svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
<g clip-path=""url(#clip0_18_80)"">
<path d=""M20.25 6V10.5M20.25 10.5H15.75M20.25 10.5L16.77 7.23C15.9639 6.42353 14.9667 5.8344 13.8714 5.51758C12.7761 5.20075 11.6183 5.16656 10.5062 5.41819C9.3941 5.66982 8.36385 6.19907 7.5116 6.95656C6.65935 7.71405 6.01288 8.67508 5.6325 9.75M3.75 18V13.5M3.75 13.5H8.25M3.75 13.5L7.23 16.77C8.03606 17.5765 9.03328 18.1656 10.1286 18.4824C11.2239 18.7992 12.3817 18.8334 13.4938 18.5818C14.6059 18.3302 15.6361 17.8009 16.4884 17.0434C17.3407 16.2859 17.9871 15.3249 18.3675 14.25"" stroke=""white"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""/>
</g>
<defs>
<clipPath id=""clip0_18_80"">
<rect width=""18"" height=""18"" fill=""white"" transform=""translate(3 3)""/>
</clipPath>
</defs>
</svg>"),
        ];




        private LinearAnimation[] _tabActiveAnimation = [new(), new(), new(), new(),];
        private LinearAnimation[] _tabSelectedAnimation = [new(), new(), new(), new(), new(), new(), new(),];
        private LinearAnimationTracker _animations = new LinearAnimationTracker();
        LinearAnimationTracker<MainTabs> _tabAnimationTracker = new();
        LinearAnimationTracker<SelectedPluginTabs> _tabSelectedAnimationTracker = new();
        Dictionary<string, bool> _isEnumOptionOpened = new();

        public PluginManagerScreen() : base("PluginManager", true, true) {
            _cancellationTokenSource = new CancellationTokenSource();
            _scrollbarPanelLogic = new ScrollbarPanelLogic();
            _scrollbarPanelLogic.MarginFromLeft = 0.0f;
            _scrollbarPanelLogic.MarginFromRight = 0f;

            _pluginSearchBox = new(32);
            _pluginSearchBox.PlaceholderText = "Search plugins...";

            Sources = new PluginSources(_cancellationTokenSource.Token);

        }

        private void OpenCurrentPlugin(IDisplayPlugin plugin) {
            _currentlyOpenedPluginIsNew = true;
            _currentlyOpenedPluginIsServer = !plugin.IsLocalVersion;
            _currentlyOpenedPluginUuid = plugin.Manifest.Uuid;
        }
        private bool CloseCurrentPlugin() {
            if (_currentlyOpenedPluginUuid is null) {
                return false;
            }

            _currentlyOpenedPluginIsNew = false;
            _currentlyOpenedPluginIsServer = false;
            _currentlyOpenedPluginUuid = null;
            return true;
        }

        private static string ShortenNumber(long num) {
            if (num >= 1_000_000_000)
                return (num / 1_000_000_000D).ToString("0.#") + "B";
            else if (num >= 1_000_000)
                return (num / 1_000_000D).ToString("0.#") + "M";
            else if (num >= 1_000)
                return (num / 1_000D).ToString("0.#") + "K";
            else
                return num.ToString();
        }

        public static string FormatDateDifferenceShort(DateTime oldDate, DateTime currentDate) {
            TimeSpan difference = currentDate - oldDate;
            if (difference.TotalDays >= 365) {
                return $"{(int)(difference.TotalDays / 365)}y";
            } else if (difference.TotalDays >= 30) {
                return $"{(int)(difference.TotalDays / 30)}mo";
            } else if (difference.TotalDays >= 1) {
                return $"{(int)difference.TotalDays}d";
            } else if (difference.TotalHours >= 1) {
                return $"{(int)difference.TotalHours}h";
            } else if (difference.TotalMinutes >= 1) {
                return $"{(int)difference.TotalMinutes}m";
            } else {
                return $"{(int)difference.TotalSeconds}s";
            }
        }

        private TexturePath? GetPluginLogoTexture(RendererCommon gfx, IDisplayPlugin plugin) {
            TexturePath texturePath = TexturePath.Assets($"{plugin.GetUniqueKey("PluginLogo")}.png");
            var status = gfx.GetTextureStatus(texturePath);
            if (status == RendererTextureStatus.Loaded) {
                return texturePath;
            } else if (status == RendererTextureStatus.Unloaded || status == RendererTextureStatus.Missing) {
                if (_logoDataTasks.TryGetValue(texturePath.Path, out var logoTask)) {
                    if (logoTask.IsCompletedSuccessfully) {
                        var logoData = logoTask.Result;
                        gfx.UploadTexture(texturePath, logoData);
                    }
                } else {
                    _logoDataTasks[texturePath.Path] = plugin.GetIconDataTask();
                }
            }

            return null;
        }
        private TexturePath? GetPluginBannerTexture(RendererCommon gfx, IDisplayPlugin plugin) {
            TexturePath texturePath = TexturePath.Assets($"{plugin.GetUniqueKey("PluginBanner")}.png");
            var status = gfx.GetTextureStatus(texturePath);
            if (status == RendererTextureStatus.Loaded) {
                return texturePath;
            } else if (status == RendererTextureStatus.Unloaded || status == RendererTextureStatus.Missing) {
                if (_bannerDataTasks.TryGetValue(texturePath.Path, out var bannerTask)) {
                    if (bannerTask.IsCompletedSuccessfully) {
                        var bannerData = bannerTask.Result;
                        gfx.UploadTexture(texturePath, bannerData);
                    }
                } else {
                    _bannerDataTasks[texturePath.Path] = plugin.GetBannerDataTask();
                }
            }

            return null;
        }

        private ColorF GetPulsatingLoadingColor(OnixClientThemeV3 theme) {
            float time = (float)_screenRuntimeTracker.Elapsed.TotalSeconds;
            float pulse = (float)(Math.Sin(time * 2 * Math.PI) * 0.5 + 0.75); // Pulsates between 0 and 1
            return theme.Highlight.Color.WithOpacity(theme.Highlight.Color.A * pulse); // Adjust opacity based on pulse
        }
        public static readonly Vec2 PluginCardSize = new Vec2(99.5f, 63.75f);
        public static readonly Vec2 PluginCardPadding = new Vec2(1f, 5f);
        public void RenderPluginCard(RendererTwoDimentional gfx, OnixClientThemeV3 theme, Rect position, IDisplayPlugin plugin) {
            float sizeOfOnePixel = Onix.Gui.GuiScaleInverse;
            var darkerColor = theme.Text.Color.WithOpacity(0.6f);
            ColorF pulsatingLoadColor = GetPulsatingLoadingColor(theme);
            gfx.FillRoundedRectangle(position, theme.Highlight, 5f, 20);
            gfx.DrawRoundedRectangle(position, theme.Outline, sizeOfOnePixel, 5f);

            

            Rect bannerRect = new Rect(position.X + 2.5f, position.Y + 2.5f, position.Z - 2.5f, position.Y + 22.5f);
            var bannerTexture = GetPluginBannerTexture(gfx, plugin);
            if (bannerTexture is not null) {
                gfx.RenderTexture(bannerRect, bannerTexture, 1.0f, Rect.FullUV);
            } else {
                gfx.FillRoundedRectangle(bannerRect, pulsatingLoadColor, 2.5f);
            }

            Rect logoRect = Rect.FromSize(bannerRect.X + 2.5f, bannerRect.W - 3.75f, 17.75f, 17.75f);
            var logoTexture = GetPluginLogoTexture(gfx, plugin);
            if (logoTexture is not null) {
                gfx.RenderTexture(logoRect, logoTexture, 1.0f, Rect.FullUV);
            } else {
                gfx.FillRoundedRectangle(logoRect, pulsatingLoadColor, 2.5f);
            }


            string titleText = plugin.Manifest.Name;
            Vec2 titlePosition = new Vec2(logoRect.Z + 2.25f, bannerRect.W - sizeOfOnePixel);
            Vec2 titleSize = gfx.MeasureText(titleText, 1.0f);
            using (_ = gfx.PushClippingRectangleWithin(new Rect(titlePosition.X, titlePosition.Y, position.Z - 2.5f, titlePosition.Y + titleSize.Y + 0.5f))) {
                gfx.RenderText(titlePosition, theme.Text, titleText, 1.0f);
            }

            string pluginVersionText = $"v{plugin.Manifest.PluginVersion}";
            if (pluginVersionText.Length > 10) {
                pluginVersionText = pluginVersionText.Substring(0, 8) + "...";
            }
            Vec2 pluginVersionTextSize = gfx.MeasureText(pluginVersionText, 0.65f);
            string authorText = $"by {plugin.Manifest.Author}";
            Vec2 authorPosition = new Vec2(logoRect.Z + 2.25f, titlePosition.Y + titleSize.Y - 1.75f);
            Vec2 authorSize = gfx.MeasureText(authorText, 0.65f);
            using (_ = gfx.PushClippingRectangleWithin(new Rect(authorPosition.X, authorPosition.Y, position.Z - 2.5f, authorPosition.Y + authorSize.Y + 0.5f))) {
                gfx.RenderText(authorPosition, darkerColor, authorText, 0.65f);
            }
            // version
            gfx.RenderText(new Vec2(position.Z - 2.5f - pluginVersionTextSize.X, authorPosition.Y), darkerColor, pluginVersionText, 0.65f);
            _versionIcon.RenderStatic(gfx, Rect.FromSize(position.Z - 2.5f - pluginVersionTextSize.X - 5.5f, authorPosition.Y + pluginVersionTextSize.Y / 2f - 2.75f, 4.5f, 4.5f), 0.6f);

            // footer

            // using the Sources one to get the download count from all remote repository combined
            string downloadCountText = ShortenNumber(Sources.GetDownloadCount(plugin.Manifest.Uuid));
            string timeSinceLastUpdateText = FormatDateDifferenceShort(plugin.LastUpdated, DateTime.Now);

            float textScale = 0.65f;
            Vec2 downloadCountTextSize = gfx.MeasureText(downloadCountText, textScale);
            Vec2 timeSinceLastUpdateTextSize = gfx.MeasureText(timeSinceLastUpdateText, textScale);
            Vec2 footerIconSize = new Vec2(5f);
            float betweenIconAndTextPadding = 1f;
            float paddingBetweenElements = 2f;

            Vec2 currentTextPos = position.BottomLeft + new Vec2(2.5f, -(2.5f + footerIconSize.Y));

            // Render download count
            _downloadCountIcon.RenderStatic(gfx, Rect.FromSize(currentTextPos, footerIconSize), 0.6f);
            gfx.RenderText(new Vec2(currentTextPos.X + footerIconSize.X + betweenIconAndTextPadding, currentTextPos.Y + footerIconSize.Y / 2 - downloadCountTextSize.Y / 2), darkerColor, downloadCountText, textScale);
            currentTextPos.X += footerIconSize.X + betweenIconAndTextPadding + downloadCountTextSize.X + paddingBetweenElements;


            // Render update time
            _updateTimeIcon.RenderStatic(gfx, Rect.FromSize(currentTextPos, footerIconSize), 0.6f);
            gfx.RenderText(new Vec2(currentTextPos.X + footerIconSize.X + betweenIconAndTextPadding, currentTextPos.Y + footerIconSize.Y / 2 - timeSinceLastUpdateTextSize.Y / 2), darkerColor, timeSinceLastUpdateText, textScale);

            float footerY = position.W - Math.Max(downloadCountTextSize.Y, Math.Max(pluginVersionTextSize.Y, Math.Max(timeSinceLastUpdateTextSize.Y, footerIconSize.Y))) - 1.0f; // 2.5f for padding
            // right button
            string rightButtonText = plugin.IsInstalled ? "Uninstall" : "Install";
            ColorF rightButtonColor = plugin.IsInstalled ? theme.Highlight.Color : theme.Button.Color;
            SvgRenderer rightButtonIcon = plugin.IsInstalled ? _trashcanIcon : _downloadCountIcon;
            if (plugin.IsBusy) {
                rightButtonText = "Busy...";
                rightButtonColor = theme.Disabled.Color;
            }
            Vec2 rightButtonTextSize = gfx.MeasureText(rightButtonText, 0.75f);
            Vec2 footerButtonIconSize = new Vec2(5f);
            Rect rightButtonRect = new Rect(position.Z - rightButtonTextSize.X - 2.5f - 4 - footerButtonIconSize.X - 2f, position.W - rightButtonTextSize.Y - 1.5f, position.Z - 2.5f, position.W - 2.5f);
            gfx.FillRoundedRectangle(rightButtonRect, rightButtonColor, 3.0f);
            gfx.RenderText(rightButtonRect.Center - rightButtonTextSize / 2f + new Vec2(footerButtonIconSize.X / 2, 0f), theme.Text.Color, rightButtonText, 0.75f);
            rightButtonIcon.RenderStatic(gfx, Rect.FromSize(rightButtonRect.X + 2.0f, rightButtonRect.Y + rightButtonRect.Height / 2 - footerButtonIconSize.Y / 2, footerButtonIconSize), 0.6f);
            bool mouseIsInRightButton = rightButtonRect.Contains(Onix.Gui.MousePosition);
            gfx.FillRoundedRectangle(rightButtonRect, theme.Highlight.Color.MultiplyOpacity(_animations.GetOrCreate(plugin.GetUniqueKey("RightButtonHoverColor")).GetLinear(mouseIsInRightButton, 0.25f)), 3.0f);

            bool ignoreClickInput = _isEnumOptionOpened.Any(x => x.Value);

            var pluginInstaller = PublicPluginManager.PluginInstaller;
            if (plugin.IsInstalled) {
                bool hasUpdate = !CompatibilityUtils.IsSamePlugin(plugin, plugin.UpdatedPlugins?.LatestCompatiblePlugin ?? plugin);
                string leftButtonText;
                ColorF leftButtonColor;
                SvgRenderer leftButtonIcon = _updateIcon;
                if (hasUpdate) {
                    leftButtonText = "Update";
                    leftButtonColor = ColorF.Orange;
                } else {
                    leftButtonText = plugin.State switch {
                        OnixRuntime.Plugin.PluginState.NotInstalled => "Install",
                        OnixRuntime.Plugin.PluginState.Unloaded => "Load",
                        OnixRuntime.Plugin.PluginState.Unloading => "Unloading...",
                        OnixRuntime.Plugin.PluginState.Loading => "Loading...",
                        OnixRuntime.Plugin.PluginState.Enabled => "Disable",
                        OnixRuntime.Plugin.PluginState.Enabling => "Enabling...",
                        OnixRuntime.Plugin.PluginState.Disabling => "Disabling...",
                        OnixRuntime.Plugin.PluginState.Disabled => "Enable",
                        _ => throw new NotImplementedException()
                    };
                    leftButtonColor = plugin.State switch {
                        OnixRuntime.Plugin.PluginState.NotInstalled => theme.Button.Color,
                        OnixRuntime.Plugin.PluginState.Unloaded => theme.Button.Color,
                        OnixRuntime.Plugin.PluginState.Unloading => theme.Blocked.Color,
                        OnixRuntime.Plugin.PluginState.Loading => theme.Blocked.Color,
                        OnixRuntime.Plugin.PluginState.Enabled => theme.Accent.Color,
                        OnixRuntime.Plugin.PluginState.Enabling => theme.Blocked.Color,
                        OnixRuntime.Plugin.PluginState.Disabling => theme.Highlight.Color,
                        OnixRuntime.Plugin.PluginState.Disabled => theme.Highlight.Color,
                        _ => throw new NotImplementedException()
                    };
                    leftButtonIcon = plugin.State switch {
                        OnixRuntime.Plugin.PluginState.NotInstalled => _downloadCountIcon,
                        OnixRuntime.Plugin.PluginState.Unloaded => _loadIcon,
                        OnixRuntime.Plugin.PluginState.Unloading => _disableIcon,
                        OnixRuntime.Plugin.PluginState.Loading => _loadIcon,
                        OnixRuntime.Plugin.PluginState.Enabled => _enableIcon,
                        OnixRuntime.Plugin.PluginState.Enabling => _enableIcon,
                        OnixRuntime.Plugin.PluginState.Disabling => _disableIcon,
                        OnixRuntime.Plugin.PluginState.Disabled => _disableIcon,
                        _ => throw new NotImplementedException()
                    };
                }
                if (!plugin.State.IsBetweenStates() && plugin.IsBusy) {
                    leftButtonText = "Busy...";
                    leftButtonColor = theme.Disabled.Color;
                }

                Vec2 leftButtonTextSize = gfx.MeasureText(leftButtonText, 0.75f);
                Rect leftButtonRect = new Rect(rightButtonRect.X - 2.5f - leftButtonTextSize.X - 4f - footerButtonIconSize.X - 1f, rightButtonRect.Y, rightButtonRect.X - 2.5f, rightButtonRect.W);
                gfx.FillRoundedRectangle(leftButtonRect, leftButtonColor, 3.0f);
                gfx.RenderText(leftButtonRect.Center - leftButtonTextSize / 2f + new Vec2(footerButtonIconSize.X / 2, 0f), theme.Text.Color, leftButtonText, 0.75f);
                leftButtonIcon.RenderStatic(gfx, Rect.FromSize(leftButtonRect.X + 2.0f, leftButtonRect.Y + leftButtonRect.Height / 2 - footerButtonIconSize.Y / 2, footerButtonIconSize), 0.6f);
                bool mouseIsInLeftButton = leftButtonRect.Contains(Onix.Gui.MousePosition);
                gfx.FillRoundedRectangle(leftButtonRect, theme.Highlight.Color.MultiplyOpacity(_animations.GetOrCreate(plugin.GetUniqueKey("LeftButtonHoverColor")).GetLinear(mouseIsInLeftButton, 0.25f)), 3.0f);

                if (ClickInput == InputKey.ClickType.Left && mouseIsInLeftButton && !ignoreClickInput) {
                    if (plugin.IsBusy) {
                        return; // Do not allow clicking while busy
                    }
                    if (hasUpdate) {
                        pluginInstaller.InstallPluginFromUrl(plugin.UpdatedPlugins?.LatestCompatiblePlugin.DownloadUrl ?? plugin.DownloadUrl, plugin.Manifest.Uuid);
                    } else {
                        if (plugin.State == OnixRuntime.Plugin.PluginState.Disabled) {
                            plugin.Enable();
                        } else if (plugin.State == OnixRuntime.Plugin.PluginState.Enabled) {
                            plugin.Disable();
                        } else if (plugin.State == OnixRuntime.Plugin.PluginState.Unloaded) {
                            plugin.StartLoadPlugin(PluginLoadMode.ForceLoadAndDisable);
                        }
                    }
                    HandleAllInputs();
                }
            }

            if (ClickInput == InputKey.ClickType.Left && mouseIsInRightButton && !ignoreClickInput) {
                if (plugin.IsBusy) {
                    return; // Do not allow clicking while busy
                }
                if (plugin.IsInstalled) {
                    pluginInstaller.UninstallPlugin(plugin.Manifest.Uuid, Onix.Input.IsDown(InputKey.Type.Shift));
                } else {
                    pluginInstaller.InstallPluginFromUrl(plugin.DownloadUrl);
                }
                HandleAllInputs();
            }
            if (position.Contains(Onix.Gui.MousePosition) && ClickInput == InputKey.ClickType.Left && !ignoreClickInput) {
                OpenCurrentPlugin(plugin);
                HandleAllInputs();
            }

            // Description
            var descriptionArea = new Rect(position.X + 2.5f, logoRect.W + 1.5f, position.Z - 2.5f, footerY - 2.5f);
            var descriptionText = gfx.WrapText(plugin.Manifest.Description, descriptionArea.Width, 0.60f);
            using (_ = gfx.PushClippingRectangleWithin(descriptionArea)) {
                gfx.RenderText(descriptionArea.TopLeft, darkerColor, descriptionText, 0.60f);
            }

        }

        private void RenderPluginGrid(RendererTwoDimentional gfx, OnixClientThemeV3 theme, Rect position, List<IDisplayPlugin> plugins) {
            int pluginsThatFitInRow = (int)((position.Width - PluginCardPadding.X) / (PluginCardSize.X + PluginCardPadding.X));
            int rowCount = (int)Math.Ceiling((float)plugins.Count / pluginsThatFitInRow);
            float totalHeight = rowCount * PluginCardSize.Y + (rowCount - 1) * PluginCardPadding.Y;
            float actualPerItemPadding = position.Width - pluginsThatFitInRow * PluginCardSize.X;
            if (totalHeight > position.Height) {
                float scrollbarWidth = _scrollbarPanelLogic.VScrollbarMaxWidth;
                pluginsThatFitInRow = (int)((position.Width - PluginCardPadding.X - scrollbarWidth) / (PluginCardSize.X + PluginCardPadding.X));
                rowCount = (int)Math.Ceiling((float)plugins.Count / pluginsThatFitInRow);
                totalHeight = rowCount * PluginCardSize.Y + (rowCount - 1) * PluginCardPadding.Y;
                actualPerItemPadding = (position.Width - scrollbarWidth) - pluginsThatFitInRow * PluginCardSize.X;
            }

            totalHeight += PluginCardPadding.X * 2; // the PluginCardPadding.X is not a bug, the padding is just weird near the top and bottom
            _scrollbarPanelLogic.ContentSize = new Vec2(position.Width, totalHeight);
            _scrollbarPanelLogic.PanelRect = position;
            _scrollbarPanelLogic.Update(ClickInput, Onix.Gui.MousePosition, ScrollCount, _deltaTime);
            float currentScrollY = _scrollbarPanelLogic.GetScrollOffset().Y;
            using (_ = gfx.PushClippingRectangleWithin(_scrollbarPanelLogic.ContentRect)) {
                actualPerItemPadding /= (pluginsThatFitInRow + 1);
                for (int pluginIndex = 0; pluginIndex < plugins.Count; pluginIndex++) {
                    int row = pluginIndex / pluginsThatFitInRow;
                    int column = pluginIndex % pluginsThatFitInRow;
                    float x = position.X + (float)column * (PluginCardSize.X + actualPerItemPadding) + actualPerItemPadding;
                    float y = position.Y + (float)row * (PluginCardSize.Y + PluginCardPadding.Y) + PluginCardPadding.X; // the PluginCardPadding.X is not a bug, the padding is just weird near the top
                    y -= currentScrollY;
                    Rect cardRect = Rect.FromSize(x, y, PluginCardSize.X, PluginCardSize.Y);
                    if (cardRect.Y > position.W || cardRect.W < position.Y) {
                        continue; // Skip rendering if the card is outside the visible area
                    }
                    RenderPluginCard(gfx, theme, cardRect, plugins[pluginIndex]);
                }
            }
            _scrollbarPanelLogic.RenderScrollbarsV3Themed(gfx);
        }

        public void RenderTabs(RendererTwoDimentional gfx, OnixClientThemeV3 theme, Rect position) {
            float paddingBetweenTabs = 3.25f;
            float roundedRectRadius = 4.75f;

            var currentTabRect = position.Shrink(paddingBetweenTabs);
            currentTabRect.W += 0.5f;
            var renderTab = (MainTabs tab, Rect remainingTabSpace) => {
                string tabText = tab switch {
                    MainTabs.Back => string.Empty,
                    MainTabs.Updates => _lastKnownUpdateCount == 0 ? "Updates" : $"Updates ({_lastKnownUpdateCount})",
                    _ => tab.ToString()
                };
                Vec2 textSize = gfx.MeasureText(tabText);

                SvgRenderer tabIcon = TabIcons[(int)tab];
                Rect iconRect = Rect.FromCenter(new(remainingTabSpace.X, remainingTabSpace.CenterY), new(6, 6));
                float inButtonPadding = (iconRect.Y - remainingTabSpace.Y);
                iconRect.X = remainingTabSpace.X + inButtonPadding;
                iconRect.Width = 6;

                Rect buttonRect = Rect.FromSize(remainingTabSpace.TopLeft, string.IsNullOrEmpty(tabText) ? (iconRect.Width + inButtonPadding * 2) : (textSize.X + iconRect.Width + inButtonPadding * 3), remainingTabSpace.Height);
                float buttonOffsetDifference = tab == MainTabs.Updates ? (remainingTabSpace.Z - buttonRect.Z) : 0f;
                buttonRect = buttonRect.MoveRight(buttonOffsetDifference);
                iconRect = iconRect.MoveRight(buttonOffsetDifference);

                bool mouseInRect = buttonRect.Contains(Onix.Gui.MousePosition);
                float activeTabAnimation = _tabActiveAnimation[(int)tab].GetLinear(_currentTab == tab, 0.25f);
                gfx.FillRoundedRectangle(buttonRect, theme.Accent.Color.MultiplyOpacity(activeTabAnimation), roundedRectRadius);
                gfx.FillRoundedRectangle(buttonRect, theme.Highlight.Color.MultiplyOpacity(activeTabAnimation * 0.35f + 0.65f), roundedRectRadius);
                if (tab == MainTabs.Discover) {
                    using (_ = gfx.PushTransformation(TransformationMatrix.RotateZ(_lastKnownPlayerYaw - 45f) * TransformationMatrix.Translate(iconRect.Center))) {
                        tabIcon.RenderStatic(gfx, Rect.FromCenter(Vec2.Zero, iconRect.Size), activeTabAnimation * 0.35f + 0.65f);
                    }
                } else if (tab == MainTabs.Updates && (mouseInRect || _currentTab == MainTabs.Updates) || (tab == MainTabs.Updates && _lastKnownUpdateCount > 0)) {
                    using (_ = gfx.PushTransformation(TransformationMatrix.RotateZ((float)(_screenRuntimeTracker.Elapsed.TotalMilliseconds / 4.0 % 360)) * TransformationMatrix.Translate(iconRect.Center))) {
                        tabIcon.RenderStatic(gfx, Rect.FromCenter(Vec2.Zero, iconRect.Size), activeTabAnimation * 0.35f + 0.65f);
                    }

                } else {
                    tabIcon.RenderStatic(gfx, iconRect, (tab == MainTabs.Back ? 1 : activeTabAnimation) * 0.35f + 0.65f);
                }
                gfx.RenderText(new Vec2(iconRect.Z + inButtonPadding / 2, buttonRect.CenterY), theme.Text.Color.MultiplyOpacity(activeTabAnimation * 0.35f + 0.65f), tabText, TextAlignment.Left, TextAlignment.Center, 1f);

                gfx.FillRoundedRectangle(buttonRect, theme.Highlight.Color.MultiplyOpacity(_tabAnimationTracker.GetOrCreate(tab).GetLinear(mouseInRect, 0.2f) + 1), roundedRectRadius);

                if (mouseInRect && ClickInput == InputKey.ClickType.Left) {
                    HandleAllInputs();
                    if (tab == MainTabs.Back) {
                        if (!CloseCurrentPlugin())
                            CloseScreen();
                    } else {
                        _currentTab = tab;
                    }
                }

                return buttonRect.Width;
            };
            if (renderTab == null) {
                throw new ArgumentNullException(nameof(renderTab));
            }

            currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(MainTabs.Back, currentTabRect) + paddingBetweenTabs);
            currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(MainTabs.Installed, currentTabRect) + paddingBetweenTabs);
            currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(MainTabs.Discover, currentTabRect) + paddingBetweenTabs);
            currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(MainTabs.Updates, currentTabRect) + paddingBetweenTabs);
        }


        private void RenderFiltersTab(RendererTwoDimentional gfx, OnixClientThemeV3 theme, Rect position) {
            float sizeOfOnePixel = Onix.Gui.GuiScaleInverse;
            float sizeAnimation = EasingAnimations.EaseInOutQuart(_animations.GetOrCreate("SearchboxSizeAnimation").GetLinear(_pluginSearchBox.IsFocused, 0.5f));
            var textboxRect = new Rect(0, position.Y, 55.75f + 40f * sizeAnimation, position.W);
            textboxRect = textboxRect.MoveRight(position.Z - (textboxRect.Z));
            var mouseInTextbox = textboxRect.Contains(Onix.Gui.MousePosition);
            if (ClickInput == InputKey.ClickType.Left) {
                _pluginSearchBox.IsFocused = mouseInTextbox;
                if (mouseInTextbox)
                    HandleAllInputs();
            }
            float iconSize = 4.5f;
            float iconPadding = (textboxRect.Height - iconSize) / 2f;
            gfx.FillRoundedRectangle(textboxRect, theme.WindowBackground, 4.75f, 20);
            gfx.DrawRoundedRectangle(textboxRect, theme.Outline, sizeOfOnePixel * 2, 4.75f, 20);
            _pluginSearchBox.Render(textboxRect.WithX(textboxRect.X + iconSize + iconPadding), theme.Text, new ColorF(0f, 0f, 0f, 0f), new ColorF(0f, 0f, 0f, 0f), OnixTextbox.CursorVisibility.Normal);
            _pluginSearchIcon.RenderStatic(gfx, Rect.FromCenter(textboxRect.X + iconPadding + iconSize / 2f, textboxRect.CenterY, iconSize, iconSize), 0.7f + sizeAnimation * 0.3f);

            // enumeration

            var renderSubMenu = ((int Number, string Text, SvgRenderer? icon)[] options, int currentOptionValue, string menuName, SvgRenderer icon, Rect enumOptionSquare) => {
                var currentOption = options.First(x => x.Number == currentOptionValue);
                Vec2 iconSizes = new Vec2(6f);
                float iconPadding = (enumOptionSquare.Height - iconSizes.Y) / 2f;
                bool mouseInRect = enumOptionSquare.Contains(Onix.Gui.MousePosition);
                var leftIconRect = Rect.FromCenter(enumOptionSquare.X + iconPadding + iconSizes.X / 2f, enumOptionSquare.CenterY, iconSizes.X, iconSizes.Y);
                var rightIconRect = Rect.FromCenter(enumOptionSquare.Z - iconPadding - iconSizes.X / 2f, enumOptionSquare.CenterY, iconSizes.X, iconSizes.Y);

                gfx.FillRoundedRectangle(enumOptionSquare, theme.Highlight.Color.MultiplyOpacity(1 + _animations.GetOrCreate(menuName + "Highlight").GetLinear(mouseInRect, 0.2f)), 4.75f, 20);
                icon.RenderStatic(gfx, leftIconRect, 1f);
                _downArrowIcon.RenderStatic(gfx, rightIconRect, 1f);
                gfx.RenderText(new Vec2(enumOptionSquare.X + iconSizes.X + iconPadding + iconPadding, enumOptionSquare.CenterY), theme.Text, currentOption.Text, TextAlignment.Left, TextAlignment.Center);

                _isEnumOptionOpened.TryAdd(menuName, false);
                bool isMenuOpened = _isEnumOptionOpened[menuName];
                if (!isMenuOpened && mouseInRect && ClickInput == InputKey.ClickType.Left) {
                    HandleAllInputs();
                    _isEnumOptionOpened[menuName] = true;
                    isMenuOpened = true;
                }

                float menuOpenAnimation = EasingAnimations.EaseInOutQuint(_animations.GetOrCreate(menuName + "Opening").GetLinear(isMenuOpened, 0.5f));
                if (isMenuOpened || menuOpenAnimation != 0) {
                    float menuHeight = options.Length * 16f;
                    var menuRect = new Rect(enumOptionSquare.X, enumOptionSquare.Y + enumOptionSquare.Height, enumOptionSquare.Z, enumOptionSquare.Y + enumOptionSquare.Height + menuHeight + 2.5f);
                    menuRect.Height = menuRect.Height * menuOpenAnimation;
                    using (_ = gfx.PushOpacity(menuOpenAnimation)) {
                        using (_ = gfx.PushClippingRectangleWithin(menuRect)) {
                            gfx.FillRoundedRectangle(menuRect, theme.WindowBackground, 4.0f, 20);
                            gfx.FillRoundedRectangle(menuRect, theme.WindowBackground, 4.0f, 20);
                            gfx.DrawRoundedRectangle(menuRect, theme.Outline, sizeOfOnePixel * 2, 4.0f, 20);

                            for (int i = 0; i < options.Length; i++) {
                                var option = options[i];
                                var optionRect = new Rect(menuRect.X + 2.5f, menuRect.Y + 2.5f + i * 16f, menuRect.Z - 2.5f, menuRect.Y + 2.5f + (i + 1) * 16f - 2f);
                                bool mouseInOption = optionRect.Contains(Onix.Gui.MousePosition);
                                gfx.FillRoundedRectangle(optionRect, theme.Highlight.Color.MultiplyOpacity(1 + _animations.GetOrCreate(menuName + "Highlight" + i).GetLinear(mouseInOption, 0.2f)), 4.75f, 20);
                                gfx.RenderText(new Vec2(optionRect.X + (option.icon is null ? 0 : iconSize + iconPadding + iconPadding), optionRect.CenterY), theme.Text.Color.MultiplyOpacity(0.8f), option.Text, TextAlignment.Left, TextAlignment.Center);
                                if (option.icon is not null) {
                                    option.icon.RenderStatic(gfx, Rect.FromCenter(optionRect.X + iconPadding + iconSize / 2f, optionRect.CenterY, iconSizes));
                                }
                                if (mouseInOption && ClickInput == InputKey.ClickType.Left) {
                                    HandleAllInputs();
                                    _isEnumOptionOpened[menuName] = false;
                                    return option.Number;
                                }
                            }
                        }
                    }
                    if (ClickInput == InputKey.ClickType.Left && !menuRect.Contains(Onix.Gui.MousePosition)) {
                        // clicked elsewhere, close the menu
                        HandleAllInputs();
                        _isEnumOptionOpened[menuName] = false;
                    }
                }

                return Int32.MaxValue;
            };


            (int, string, SvgRenderer? icon)[] relevancyOptions = [
                ((int)PluginRelevances.Popular, "Popular", _popularRelevancyIcon),
                ((int)PluginRelevances.New, "New", _newRelevancyIcon),
                ((int)PluginRelevances.AToZ, "A-Z", _aToZRelevancyIcon),
                ((int)PluginRelevances.ZToA, "Z-A", _zToARelevancyIcon)
            ];
            var currentRelevanceEnum = _currentTab == MainTabs.Discover ? _pluginRelevanceDiscover : _pluginRelevance;
            int currentRelevance = (int)currentRelevanceEnum;
            var selectedRelevanceIcon = currentRelevanceEnum switch {
                PluginRelevances.Popular => _popularRelevancyIcon,
                PluginRelevances.New => _newRelevancyIcon,
                PluginRelevances.AToZ => _aToZRelevancyIcon,
                PluginRelevances.ZToA => _zToARelevancyIcon,
                _ => _relevanceIcon
            };
            var pluginRelevanceRect = Rect.FromSize(position.TopLeft, 51.25f, position.Height);
            int newlySelectedRelevance = renderSubMenu(relevancyOptions, currentRelevance, "Relevance", selectedRelevanceIcon, pluginRelevanceRect);
            if (newlySelectedRelevance != Int32.MaxValue) {
                if (_currentTab == MainTabs.Discover)
                    _pluginRelevanceDiscover = (PluginRelevances)newlySelectedRelevance;
                else
                    _pluginRelevance = (PluginRelevances)newlySelectedRelevance;
            }

        }


        public IEnumerable<IDisplayPlugin> GetPluginsForFrame() {
            IEnumerable<IDisplayPlugin> result = Sources.GetPluginsForFrame();
            _lastKnownUpdateCount = result.Count(plugin => plugin.IsInstalled && CompatibilityUtils.IsNewerPlugin(plugin.UpdatedPlugins?.LatestCompatiblePlugin ?? plugin, plugin));
            switch (_currentTab) {
                case MainTabs.Installed:
                    result = result.Where(plugin => plugin.IsInstalled && plugin.IsLocalVersion);
                    break;
                case MainTabs.Discover:
                    result = result.Where(plugin => !plugin.IsLocalVersion);
                    break;
                case MainTabs.Updates:
                    result = result.Where(plugin => plugin.IsInstalled && CompatibilityUtils.IsNewerPlugin(plugin.UpdatedPlugins?.LatestCompatiblePlugin ?? plugin, plugin));
                    break;
            }
            string lowerInvariantSearchText = _pluginSearchBox.Text.ToLowerInvariant();
            if (lowerInvariantSearchText.Length > 0) {
                result = result.Where(plugin => plugin.Manifest.Name.ToLowerInvariant().Contains(lowerInvariantSearchText));
            }

            // Helper for search index sorting
            Func<IDisplayPlugin, int> searchIndex = plugin =>
                plugin.Manifest.Name.ToLowerInvariant().IndexOf(lowerInvariantSearchText);

            if (_currentTab != MainTabs.Discover) {
                result = _pluginRelevance switch {
                    PluginRelevances.Popular => result
                        .OrderBy(searchIndex)
                        .ThenByDescending(plugin => Sources.GetDownloadCount(plugin.Manifest.Uuid)),
                    PluginRelevances.New => result
                        .OrderBy(searchIndex)
                        .ThenByDescending(plugin => plugin.LastUpdated),
                    PluginRelevances.AToZ => result
                        .OrderBy(searchIndex)
                        .ThenBy(plugin => plugin.Manifest.Name),
                    PluginRelevances.ZToA => result
                        .OrderBy(searchIndex)
                        .ThenByDescending(plugin => plugin.Manifest.Name),
                    _ => result
                };
            } else {
                result = _pluginRelevanceDiscover switch {
                    PluginRelevances.Popular => result
                        .OrderBy(searchIndex)
                        .ThenByDescending(plugin => Sources.GetDownloadCount(plugin.Manifest.Uuid)),
                    PluginRelevances.New => result
                        .OrderBy(searchIndex)
                        .ThenByDescending(plugin => plugin.LastUpdated),
                    PluginRelevances.AToZ => result
                        .OrderBy(searchIndex)
                        .ThenBy(plugin => plugin.Manifest.Name),
                    PluginRelevances.ZToA => result
                        .OrderBy(searchIndex)
                        .ThenByDescending(plugin => plugin.Manifest.Name),
                    _ => result
                };
            }

            return result;
        }

        public void RenderSelectedPluginTabs(RendererTwoDimentional gfx, OnixClientThemeV3 theme, Rect position) {
            var plugin = Sources.GetPluginByUuid(_currentlyOpenedPluginUuid ?? "", _currentlyOpenedPluginIsServer);
            float paddingBetweenTabs = 3.25f;
            float roundedRectRadius = 4.75f;

            var currentTabRect = position.Shrink(paddingBetweenTabs);
            currentTabRect.W += 0.5f;
            var renderTab = (SelectedPluginTabs tab, Rect remainingTabSpace) => {
                string tabText = tab switch {
                    SelectedPluginTabs.Back => string.Empty,
                    SelectedPluginTabs.InstallUninstall => plugin?.IsInstalled ?? false ? "Uninstall" : "Install",
                    SelectedPluginTabs.LoadUnload => plugin?.IsLoaded ?? false ? "Unload" : "Load",
                    SelectedPluginTabs.EnableDisable => plugin?.State switch {
                        OnixRuntime.Plugin.PluginState.Disabled => "Enable",
                        OnixRuntime.Plugin.PluginState.Disabling => "Enable",
                        OnixRuntime.Plugin.PluginState.Enabled => "Disable",
                        OnixRuntime.Plugin.PluginState.Enabling => "Disable",
                        _ => plugin?.IsLoaded ?? false ? "Disable" : "Enable"
                    },
                    SelectedPluginTabs.OnlineView => "Online View",
                    SelectedPluginTabs.PackagePlugin => "Create Package",
                    SelectedPluginTabs.Update => "Update",
                    _ => tab.ToString()
                };
                Vec2 textSize = gfx.MeasureText(tabText);

                SvgRenderer tabIcon = tab switch {
                    SelectedPluginTabs.Back => TabIcons[(int)MainTabs.Back],
                    SelectedPluginTabs.InstallUninstall => plugin?.IsInstalled ?? false ? _trashcanIcon : _downloadCountIcon,
                    SelectedPluginTabs.LoadUnload => plugin?.IsLoaded ?? false ? _unloadIcon : _loadIcon,
                    SelectedPluginTabs.EnableDisable => plugin?.State switch {
                        OnixRuntime.Plugin.PluginState.Disabled => _disableIcon,
                        OnixRuntime.Plugin.PluginState.Disabling => _disableIcon,
                        OnixRuntime.Plugin.PluginState.Enabled => _enableIcon,
                        OnixRuntime.Plugin.PluginState.Enabling => _enableIcon,
                        _ => _disableIcon
                    },
                    SelectedPluginTabs.PackagePlugin => _versionIcon,
                    SelectedPluginTabs.OnlineView => _onlineViewIcon,
                    SelectedPluginTabs.Update => _updateIcon,
                    _ => throw new NotImplementedException()
                };
                Rect iconRect = Rect.FromCenter(new(remainingTabSpace.X, remainingTabSpace.CenterY), new(6, 6));
                float inButtonPadding = (iconRect.Y - remainingTabSpace.Y);
                iconRect.X = remainingTabSpace.X + inButtonPadding;
                iconRect.Width = 6;

                Rect buttonRect = Rect.FromSize(remainingTabSpace.TopLeft, string.IsNullOrEmpty(tabText) ? (iconRect.Width + inButtonPadding * 2) : (textSize.X + iconRect.Width + inButtonPadding * 3), remainingTabSpace.Height);
                float buttonOffsetDifference = tab == SelectedPluginTabs.OnlineView ? (remainingTabSpace.Z - buttonRect.Z) : 0f;
                buttonRect = buttonRect.MoveRight(buttonOffsetDifference);
                iconRect = iconRect.MoveRight(buttonOffsetDifference);

                bool mouseInRect = buttonRect.Contains(Onix.Gui.MousePosition);
                bool currentTabActivated = tab switch {
                    SelectedPluginTabs.Back => false,
                    SelectedPluginTabs.InstallUninstall => false,
                    SelectedPluginTabs.LoadUnload => !(plugin?.IsLoaded ?? false),
                    SelectedPluginTabs.EnableDisable => plugin?.State switch {
                        OnixRuntime.Plugin.PluginState.Disabled => true,
                        OnixRuntime.Plugin.PluginState.Disabling => true,
                        OnixRuntime.Plugin.PluginState.Enabled => false,
                        OnixRuntime.Plugin.PluginState.Enabling => false,
                        _ => false
                    },
                    SelectedPluginTabs.OnlineView => _currentlyOpenedPluginIsServer,
                    _ => false
                };
                float activeTabAnimation = _tabSelectedAnimation[(int)tab].GetLinear(currentTabActivated, 0.25f);
                var tabButtonColor = theme.Accent.Color.MultiplyOpacity(activeTabAnimation);
                if (tab == SelectedPluginTabs.Update)
                    tabButtonColor = ColorF.Orange;
                gfx.FillRoundedRectangle(buttonRect, tabButtonColor, roundedRectRadius);
                gfx.FillRoundedRectangle(buttonRect, theme.Highlight.Color.MultiplyOpacity(activeTabAnimation * 0.35f + 0.65f), roundedRectRadius);

                tabIcon.RenderStatic(gfx, iconRect, (tab == SelectedPluginTabs.Back ? 1 : activeTabAnimation) * 0.35f + 0.65f);

                gfx.RenderText(new Vec2(iconRect.Z + inButtonPadding / 2, buttonRect.CenterY), theme.Text.Color.MultiplyOpacity(activeTabAnimation * 0.35f + 0.65f), tabText, TextAlignment.Left, TextAlignment.Center, 1f);

                gfx.FillRoundedRectangle(buttonRect, theme.Highlight.Color.MultiplyOpacity(_tabSelectedAnimationTracker.GetOrCreate(tab).GetLinear(mouseInRect, 0.2f) + 1), roundedRectRadius);

                if (mouseInRect && ClickInput == InputKey.ClickType.Left) {
                    HandleAllInputs();
                    if (tab == SelectedPluginTabs.Back) {
                        if (!CloseCurrentPlugin())
                            CloseScreen();
                    } else if (tab == SelectedPluginTabs.EnableDisable) {
                        if (plugin?.IsInstalled ?? false && !(plugin?.IsBusy ?? false)) {
                            if (plugin?.State == PluginState.Enabled) {
                                plugin.Disable();
                            } else if (plugin?.State == PluginState.Disabled) {
                                plugin.Enable();
                            }
                        }
                    } else if (tab == SelectedPluginTabs.LoadUnload) {
                        if (plugin?.IsInstalled ?? false && !(plugin?.IsBusy ?? false)) {
                            if (plugin?.IsLoaded ?? false) {
                                plugin.StartUnloadPlugin(false);
                            } else {
                                plugin?.StartLoadPlugin(PluginLoadMode.ForceLoadAndDisable);
                            }
                        }
                    } else if (tab == SelectedPluginTabs.InstallUninstall) {
                        if (!(plugin?.IsBusy ?? false)) {
                            if (plugin?.IsInstalled ?? false) {
                                _currentlyOpenedPluginIsServer = true;
                                _currentlyOpenedPluginIsNew = true;
                                PublicPluginManager.PluginInstaller.UninstallPlugin(plugin.Manifest.Uuid, Onix.Input.IsDown(InputKey.Type.Shift));
                            } else {
                                _currentlyOpenedPluginIsServer = false;
                                _currentlyOpenedPluginIsNew = true;
                                PublicPluginManager.PluginInstaller.InstallPluginFromUrl(plugin?.DownloadUrl ?? "", plugin?.Manifest.Uuid ?? "");
                            }
                        }
                    } else if (tab == SelectedPluginTabs.OnlineView) {
                            _currentlyOpenedPluginIsServer = !_currentlyOpenedPluginIsServer;
                    } else if (tab == SelectedPluginTabs.PackagePlugin) {
                        if (!(plugin?.IsBusy ?? false)) {
                            var packageTask = PublicPluginManager.PluginInstaller.PackagePlugin(plugin!.Manifest.Uuid);
                            var byteTask = packageTask.Task as Task<byte[]>;
                            byteTask?.ContinueWith(packageBytes => {
                                string packagesPath = Path.Combine(OnixPluginManager.Instance.PluginPersistentDataPath, "PackagedPlugins");
                                if (!Directory.Exists(packagesPath)) {
                                    Directory.CreateDirectory(packagesPath);
                                }
                                string packageFileName = $"{plugin.Manifest.Name.Replace("(", "").Replace(")", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace(":", "").Replace("/", "").Replace("\\", "").Replace("|", "").Replace("?", "").Replace("*", "")}.onixplugin";
                                var newPackagePath = Path.Combine(packagesPath, packageFileName);
                                File.WriteAllBytes(newPackagePath, packageBytes.Result);
                                Clipboard.InitializeOle();
                                Clipboard.SetFile(newPackagePath);
                                Onix.Client.NotifyBanner("Plugin Packaged Successfully", "The plugin package has been copied to your clipboard, paste it where you want the file.", 8f);
                            });
                        }
                    } else if (tab == SelectedPluginTabs.Update) {
                        if (plugin?.IsInstalled ?? false && !(plugin?.IsBusy ?? false)) {
                            var latestCompatiblePlugin = plugin.UpdatedPlugins?.LatestCompatiblePlugin;
                            if (latestCompatiblePlugin is not null && CompatibilityUtils.IsNewerPlugin(latestCompatiblePlugin, plugin)) {
                                PublicPluginManager.PluginInstaller.InstallPluginFromUrl(latestCompatiblePlugin.DownloadUrl, plugin.Manifest.Uuid);
                            } else {
                                Onix.Client.NotifyBanner("No Updates Available", "The plugin is already up to date.", 5f);
                            }
                        }
                    }
                }

                return buttonRect.Width;
            };
            if (renderTab == null) {
                throw new ArgumentNullException(nameof(renderTab));
            }

            currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.Back, currentTabRect) + paddingBetweenTabs);
            if (plugin is not null && !plugin.IsBusy) {
                if (plugin.IsInstalled) {
                    if (plugin.IsLoaded) {
                        currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.EnableDisable, currentTabRect) + paddingBetweenTabs);
                    }
                    currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.LoadUnload, currentTabRect) + paddingBetweenTabs);
                }
                if (plugin.IsInstalled) {
                    currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.PackagePlugin, currentTabRect) + paddingBetweenTabs);
                }
                currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.InstallUninstall, currentTabRect) + paddingBetweenTabs);
                if (plugin.IsInstalled && !CompatibilityUtils.IsSamePlugin(plugin, plugin.UpdatedPlugins?.LatestCompatiblePlugin ?? plugin))
                    currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.Update, currentTabRect) + paddingBetweenTabs);
                currentTabRect = currentTabRect.WithX(currentTabRect.X + renderTab(SelectedPluginTabs.OnlineView, currentTabRect) + paddingBetweenTabs);
            }
        }

        public void RenderPluginPage(RendererTwoDimentional gfx, OnixClientThemeV3 theme, Rect position) {
            var plugin = Sources.GetPluginByUuid(_currentlyOpenedPluginUuid ?? "", _currentlyOpenedPluginIsServer);
            if (plugin is null) return;
            if (plugin.IsLocalVersion) {
                if (_currentlyOpenedPluginIsNew || (_settingListRenderer.ModuleSource != plugin.DisplayModule)) {
                    _settingListRenderer.ModuleSource = plugin.DisplayModule;
                    _currentlyOpenedPluginIsNew = false;
                }
                _settingListRenderer.Render(position, ClickInput, ScrollCount);
                if (position.Contains(Onix.Gui.MousePosition)) {
                    HandleAllInputs();
                }
            } else {
                if (_currentlyOpenedPluginIsNew) {
                    //TOOD: Reload contents when not dynamically loaded
                    _currentlyOpenedPluginIsNew = false;
                }
                using (_ = gfx.PushClippingRectangleWithin(position)) {
                    Vec2 titleTextSize = gfx.MeasureText(plugin.Manifest.Name, 2);
                    gfx.RenderText(position.TopLeft + new Vec2(6f, -1.0f), theme.Text, plugin.Manifest.Name, TextAlignment.Left, TextAlignment.Top, 2f);
                    var wrappedDescription = gfx.WrapText(plugin.Manifest.Description, position.Width - 5f, 0.7f);
                    gfx.RenderText(position.TopLeft + new Vec2(6f, -1.0f + titleTextSize.Y), theme.Text, wrappedDescription, TextAlignment.Left, TextAlignment.Top, 0.7f);
                }
            }
        }


        public void RenderScreenContents(RendererTwoDimentional gfx, bool closing) {
            gfx.FontUsage = FontUsage.UserInterface;
            float timeSinceOpened = (float)_screenRuntimeTracker.Elapsed.TotalSeconds;
            float sizeOfOnePixel = Onix.Gui.GuiScaleInverse;
            var theme = Onix.Client.ThemeV3;
            _deltaTime = Math.Min((float)_deltaTimeTracker.Elapsed.TotalSeconds, 1f);
            _deltaTimeTracker.Restart();

            Rect mainWindowBackgroundRect = Rect.FromCenter(RenderArea.Center, RenderArea.Width * 0.65416666666666f, RenderArea.Height * 0.74444444444444f);

            
            using (_ = gfx.PushTransformation(TransformationMatrix.Translate(-mainWindowBackgroundRect.Center) * TransformationMatrix.Scale(closing ? EasingAnimations.EaseOutExpo((0.23f - Math.Min(timeSinceOpened, 0.23f)) / 0.23f) : EasingAnimations.EaseInExpo(Math.Min(timeSinceOpened, 0.23f) / 0.23f)) * TransformationMatrix.Translate(mainWindowBackgroundRect.Center))) {
                using (_ = gfx.PushOpacity(EasingAnimations.Linear((closing ? 0.3f - Math.Min(timeSinceOpened, 0.3f) : Math.Min(timeSinceOpened, 0.3f)) / 0.3f))) {
                    gfx.FillRoundedRectangle(mainWindowBackgroundRect, theme.WindowBackground, 7.5f);
                    gfx.DrawRoundedRectangle(mainWindowBackgroundRect, theme.Outline, sizeOfOnePixel, 7.5f);

                    var tabsRect = mainWindowBackgroundRect.WithW(mainWindowBackgroundRect.Y + 17.5f);
                    tabsRect.W += sizeOfOnePixel;
                    if (_currentlyOpenedPluginUuid is not null) {
                        RenderSelectedPluginTabs(gfx, theme, tabsRect);
                    } else {
                        RenderTabs(gfx, theme, tabsRect);
                    }
                    var tabsRectSplitter = tabsRect.Shrink(sizeOfOnePixel).WithHeight(sizeOfOnePixel).MoveDown(tabsRect.Height);
                    tabsRectSplitter.Y = MathF.Ceiling(tabsRectSplitter.Y) - sizeOfOnePixel * 2f;
                    tabsRectSplitter.Height = sizeOfOnePixel;
                    gfx.FillRectangle(tabsRectSplitter, theme.Outline); // splitter


                    if (_currentlyOpenedPluginUuid is null) {
                        var filteringRect = tabsRectSplitter.WithHeight(tabsRect.Height - sizeOfOnePixel).MoveDown(sizeOfOnePixel);
                        var filteringRectSplitter = filteringRect.WithHeight(sizeOfOnePixel).MoveDown(filteringRect.Height);
                        gfx.FillRectangle(filteringRectSplitter, theme.Outline); // splitter


                        Rect totalGridArea = mainWindowBackgroundRect.Shrink(2.25f);
                        totalGridArea.Y += 35f;
                        var plugins = GetPluginsForFrame().ToList();
                        RenderPluginGrid(gfx, theme, totalGridArea, plugins);


                        RenderFiltersTab(gfx, theme, filteringRect.Shrink(2.5f));
                    } else {
                        RenderPluginPage(gfx, theme, mainWindowBackgroundRect.WithY(mainWindowBackgroundRect.Y + 18f));
                    }
                }
            }
            HandleAllInputs();
        }


        public override void OnRender(RendererTwoDimentional gfx) {
            RenderScreenContents(gfx, false);
        }
        public override void OnRenderClosing(RendererTwoDimentional gfx) {
            RenderScreenContents(gfx, true);
        }


        public override void OnRenderGame(RendererGame gfx) {
            if (Onix.LocalPlayer is not null) _lastKnownPlayerYaw = Onix.LocalPlayer.CardinalHeadRotation;
        }

        public override void OnOpened() {
            base.OnOpened();
            _settingListRenderer.ModuleSource = null;
            _currentlyOpenedPluginUuid = null;
            _currentlyOpenedPluginIsServer = false;
            _currentlyOpenedPluginIsNew = true;
            _lastKnownUpdateCount = 0;
            _deltaTimeTracker.Restart();
            _tabSelectedAnimationTracker.ResetAll();
            _tabAnimationTracker.ResetAll();
            _animations.ResetAll();
            _isEnumOptionOpened.Clear();
            _pluginSearchBox.Text = string.Empty;
            _cancellationTokenSource = new();
            Sources = new PluginSources(_cancellationTokenSource.Token);
            _screenRuntimeTracker.Restart();
        }

        public override bool OnClosed() {
            if (CloseCurrentPlugin()) return true;
            _cancellationTokenSource.Cancel();
            _screenRuntimeTracker.Restart();
            PublicPluginManager.SaveRelevantPluginsInTheBackground();
            return false;
        }
        public override float GetCloseAnimationDuration() {
            return 0.25f;
        }
        public override void OnCloseFinished() {
            base.OnCloseFinished();
            _settingListRenderer.ModuleSource = null;
        }

    }

}