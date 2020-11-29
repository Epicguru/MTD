using Microsoft.Xna.Framework;
using Nez;

namespace MTD.Components
{
    public static class HealthBarRenderer
    {
        public static Vector2 Size = new Vector2(200, 30);
        public static float OutlineSize = 5;

        public static void Render(Batcher batcher, Camera camera, Vector2 center, int current, int max)
        {
            float percentage = (float)current / max;
            RectangleF rect = RectangleF.Centered(center, Size);
            batcher.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, Color.White);

            rect.Inflate(-OutlineSize, -OutlineSize);

            batcher.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, Color.Red);
            batcher.DrawRect(rect.X, rect.Y, rect.Width * percentage, rect.Height, Color.ForestGreen);

            if (camera.RawZoom >= 0.25f)
            {
                var text = $"{current} / {max}";
                var size = Main.Font24.MeasureString(text);
                batcher.DrawString(Main.Font24, text, center - size * 0.5f, Color.White);
            }
        }
    }
}
