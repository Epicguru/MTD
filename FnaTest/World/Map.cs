using System;
using Microsoft.Xna.Framework;
using Nez;
using System.Runtime.CompilerServices;
using MTD.World.Pathfinding;

namespace MTD.World
{
    public class Map : RenderableComponent, IUpdatable
    {
        public static Map Current { get; private set; }

        public override float Width => WidthInTiles * Tile.SIZE;
        public override float Height => HeightInTiles * Tile.SIZE;

        public TileLayer[] Layers { get; private set; }
        public readonly int WidthInTiles, HeightInTiles;
        public Point MouseTileCoordinates { get; private set; }

        public Map(int width, int height)
        {
            WidthInTiles = width;
            HeightInTiles = height;

            Layers = new TileLayer[1];
            Layers[0] = new TileLayer(width, height);
        }

        public void Update()
        {
            MouseTileCoordinates = WorldToTileCoordinates(Input.WorldMousePos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point WorldToTileCoordinates(Vector2 worldPos)
        {
            return ((worldPos / Tile.SIZE).Round()).ToPoint();
        }

        /// <summary>
        /// Given a tile coordinate, returns the world position of the tile center.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 TileToWorldPosition(Point tilePos)
        {
            return tilePos.ToVector2() * Tile.SIZE;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            Current = this;
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            Current = null;
        }

        /// <summary>
        /// Checks weather the specified x, y coordinates are within the map bounds.
        /// </summary>
        /// <param name="x">The tile X position.</param>
        /// <param name="y">The tile Y position.</param>
        /// <returns>True if within bounds, false if outside of bounds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < WidthInTiles && y >= 0 && y < HeightInTiles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tile GetTile(int x, int y, int layer)
        {
            if (layer < 0 || layer >= Layers.Length)
                return null;

            return Layers[layer].GetTile(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWalkable(int x, int y)
        {
            return IsInBounds(x, y) && Layers[0].GetTileFast(x, y) == null;
        }

        public Tile SetTile(int x, int y, int layer, TileDef tile)
        {
            if (layer < 0 || layer >= Layers.Length)
                return null;

            return Layers[layer].SetTile(x, y, tile);
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                var layer = Layers[i];
                layer.Render(batcher, camera, (float)i / Layers.Length);
            }

            batcher.DrawHollowRect(new Rectangle(MouseTileCoordinates.X * Tile.SIZE - Tile.SIZE / 2, MouseTileCoordinates.Y * Tile.SIZE - Tile.SIZE / 2, Tile.SIZE, Tile.SIZE), Color.Red, 1);
        }
    }
}
