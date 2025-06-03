using OnixRuntime.Api.Maths;
using OnixRuntime.Api.Utils;

namespace OnixPluginManager {
    public class BannerLogoHelpers {
        public static Vec2I LogoSize => new(512, 512);
        public static Vec2I BannerSize => new(1890, 400);
        
        private static int GetDeterministicIntHash(string input) {
            const uint fnvOffset = 2166136261;
            const uint fnvPrime = 16777619;

            uint hash = fnvOffset;
            foreach (char c in input) {
                hash ^= c;
                hash *= fnvPrime;
            }

            return unchecked((int)hash); // cast to signed int (32-bit)
        }
        
        public static async Task<RawImageData> PostProcessLogo(RawImageData? image, string uuid) {
            var newImage = image ?? RawImageData.CreateRandomGradient(LogoSize.X, LogoSize.Y, new Random(GetDeterministicIntHash(uuid)));
            if (newImage.Width != LogoSize.X || newImage.Height != LogoSize.Y) {
                newImage = await Task.Run(() => newImage.Resized(LogoSize.X, LogoSize.Y));
            }
            await Task.Run(() => newImage.RoundImageCorners(72f));
            return newImage;
        }
        public static async Task<RawImageData> PostProcessBanner(RawImageData? image, string uuid) {
            var newImage = image ?? RawImageData.CreateRandomGradient(BannerSize.X, BannerSize.Y, new Random(GetDeterministicIntHash(uuid)));
            if (newImage.Width != BannerSize.X || newImage.Height != BannerSize.Y) {
                newImage = await Task.Run(() => newImage.Resized(BannerSize.X, BannerSize.Y));
            }
            await Task.Run(() => newImage.RoundImageCorners(50f));
            return newImage;
        }
    }
}