using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Textures;

namespace MTD.World
{
    public class TileLayer
    {
        public readonly int WidthInTiles, HeightInTiles;
        public int RenderedTileCount { get; private set; }

        private Tile[] tiles;

        public TileLayer(int tileWidth, int tileHeight)
        {
            WidthInTiles = tileWidth;
            HeightInTiles = tileHeight;

            tiles = new Tile[WidthInTiles * HeightInTiles];
        }

        /// <summary>
        /// Gets the internal tile index given an X and Y tile coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTileIndex(int x, int y)
        {
            return x + y * WidthInTiles;
        }

        /// <summary>
        /// Checks weather the specified x, y coordinates are within the layer bounds.
        /// </summary>
        /// <param name="x">The tile X position.</param>
        /// <param name="y">The tile Y position.</param>
        /// <returns>True if within bounds, false if outside of bounds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < WidthInTiles && y >= 0 && y < HeightInTiles;
        }

        /// <summary>
        /// Gets a tile at the specified coordinate.
        /// Will return null if the position is out of bounds or if the tile is null (air).
        /// </summary>
        /// <param name="x">The tile X position.</param>
        /// <param name="y">The tile Y position.</param>
        /// <returns>The Tile, or null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tile GetTile(int x, int y)
        {
            return IsInBounds(x, y) ? GetTileFast(x, y) : null;
        }

        /// <summary>
        /// A faster but unsafe version of <see cref="GetTile(int, int)"/> that does not
        /// check if the coordinates are out of bounds.
        /// Only use if you are 100% sure that the coordinates are within bounds!
        /// </summary>
        /// <param name="x">The tile X position.</param>
        /// <param name="y">The tile Y position.</param>
        /// <returns>The Tile, or null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tile GetTileFast(int x, int y)
        {
            return tiles[GetTileIndex(x, y)];
        }

        internal void Tick(int index)
        {
            tiles[index]?.Tick();
        }

        public Tile SetTile(int x, int y, TileDef tile, bool fromLoadOrUnload = false)
        {
            if (!IsInBounds(x, y))
                return null;
            
            Tile spawned = tile?.CreateTile(x, y);
            SetTileFast(GetTileIndex(x, y), spawned, fromLoadOrUnload);
            return spawned;
        }

        private void SetTileFast(int index, Tile tile, bool fromLoadOrUnload)
        {
            var old = tiles[index];
            old?.OnRemoved(fromLoadOrUnload);

            if(tile != null)
                tile.Layer = this;

            tiles[index] = tile;
            tile?.OnPlaced(fromLoadOrUnload);
        }

        public void Render(Batcher batcher, Camera camera, float depth)
        {
            var camBounds = camera.Bounds;
            int sx = Mathf.Clamp(Mathf.FloorToInt(camBounds.X / Tile.SIZE), 0, WidthInTiles);
            int sy = Mathf.Clamp(Mathf.FloorToInt(camBounds.Y / Tile.SIZE), 0, HeightInTiles);
            int ex = Mathf.Clamp(Mathf.CeilToInt(camBounds.Right / Tile.SIZE) + 1, 0, WidthInTiles);
            int ey = Mathf.Clamp(Mathf.CeilToInt(camBounds.Bottom / Tile.SIZE) + 1, 0, HeightInTiles);

            for (int x = sx; x < ex; x++)
            {
                for (int y = sy; y < ey; y++)
                {
                    var tile = GetTileFast(x, y);
                    tile?.Draw(batcher, camera, depth);
                }
            }

            RenderedTileCount = (ex - sx) * (ey - sy);
        }
    }
}
