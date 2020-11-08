using Microsoft.Xna.Framework;
using Nez;

namespace MTD.Components
{
    public class BoxDrawer : RenderableComponent
    {
        public override RectangleF Bounds
        {
            get
            {
                var pos = base.Entity?.Position ?? Vector2.Zero;
                pos += LocalOffset;
                var size = Size;

                return new RectangleF(pos - new Vector2(size.X * OriginNormalized.X, size.Y * OriginNormalized.Y), size);
            }
        }

        public float Thickness = 1f;
        public Vector2 Size = new Vector2(32, 32);
        public Vector2 OriginNormalized = new Vector2(0.5f, 0.5f);

        public override void Render(Batcher batcher, Camera camera)
        {
            batcher.DrawHollowRect(Bounds, base.Color, Thickness);
        }
    }
}
