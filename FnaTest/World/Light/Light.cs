using Microsoft.Xna.Framework;
using Nez;

namespace MTD.World.Light
{
    /// <summary>
    /// Represents a light that interacts with the world's tiles or entities.
    /// </summary>
    public abstract class Light
    {
        public virtual Vector2 Position { get; set; }
        public bool Enabled = true;
        public virtual Color Color { get; set; } = Color.White;
        public bool IsLightDirty { get; protected set; }

        public abstract void Recalculate();
        public abstract void Render(Batcher batcher, Camera camera);

        public struct Tile
        {
            public readonly int X, Y;
            public readonly int Index;
            public Color Color;
            public float Dst;
            public float Scale;

            public Tile(int x, int y, Color color, float scale = 2f)
            {
                this.X = x;
                this.Y = y;
                this.Index = x + y * Map.Current.WidthInTiles;
                this.Color = color;
                this.Scale = scale;
                this.Dst = 0;
            }

            public void Render(Batcher batcher, Camera camera)
            {
                float size = World.Tile.SIZE * Scale;
                float x = X * World.Tile.SIZE - size * 0.5f;
                float y = Y * World.Tile.SIZE - size * 0.5f;
                batcher.DrawRect(x, y, size, size, Color);
                //Debug.DrawText(Graphics.Instance.BitmapFont, Dst.ToString(), new Vector2(World.Tile.SIZE * X - World.Tile.SIZE * 0.5f, World.Tile.SIZE * Y - World.Tile.SIZE * 0.5f), Color.Black, scale: 3);
            }
        }
    }
}
