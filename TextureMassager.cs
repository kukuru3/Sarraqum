using Ur;

namespace Sarraqum {
    class TextureMassager {
        public void AddAlphaChannel(Sargon.Assets.Texture sourceTexture) {
            var pixels = sourceTexture.GetPixels();
            foreach (var pixel in pixels.Iterate()) {
                if (pixel.Value.a < 0.99f) continue;
                var alpha = 1f;
                if (pixel.Value.r + pixel.Value.g + pixel.Value.b < float.Epsilon) {
                    alpha = 0f;
                }
                pixels[pixel.X, pixel.Y].a = alpha;
            }
            sourceTexture.SetPixels(pixels);
        }
    }
}
