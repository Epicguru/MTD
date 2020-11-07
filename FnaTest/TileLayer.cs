using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MTD.World;
using Nez;
using Nez.Textures;

namespace MTD
{
    public class TileLayer : RenderableComponent
    {
        public readonly int WidthInTiles, HeightInTiles;
        public int RenderedTileCount { get; private set; }

        public override float Width => WidthInTiles * Tile.SIZE;
        public override float Height => HeightInTiles * Tile.SIZE;

        public TileLayer(int tileWidth, int tileHeight)
        {
            WidthInTiles = tileWidth;
            HeightInTiles = tileHeight;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            var camBounds = camera.Bounds;
            int sx = Mathf.Clamp(Mathf.FloorToInt(camBounds.X / Tile.SIZE), 0, WidthInTiles);
            int sy = Mathf.Clamp(Mathf.FloorToInt(camBounds.Y / Tile.SIZE), 0, HeightInTiles);
            int ex = Mathf.Clamp(Mathf.CeilToInt(camBounds.Right / Tile.SIZE), 0, WidthInTiles);
            int ey = Mathf.Clamp(Mathf.CeilToInt(camBounds.Bottom / Tile.SIZE), 0, HeightInTiles);

            Sprite toDraw = Entity.Scene.Content.LoadSpriteAtlas("./Content/MainAtlas.atlas").GetSprite("Tiles/Dirt");
            Sprite toDraw2 = Entity.Scene.Content.LoadSpriteAtlas("./Content/MainAtlas.atlas").GetSprite("Tiles/Stone");
            
            var start = Transform.Position;
            for (int x = sx; x < ex; x++)
            {
                for (int y = sy; y < ey; y++)
                {
                    var spr = (x + y) % 2 == 0 ? toDraw : toDraw2;
                    batcher.Draw(spr, start + new Vector2(x * Tile.SIZE, y * Tile.SIZE), base.Color, 0f, spr.Origin, 1f, SpriteEffects.None, this._layerDepth);
                }
            }

            RenderedTileCount = (ex - sx) * (ey - sy);
        }
    }
}
