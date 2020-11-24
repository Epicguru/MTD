using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MTD.Scenes;
using Nez;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        private EffectParameter tileShaderMaskParam, tileShaderMatrixParam;

        private Collider[] colliders;
        private Queue<(int, int, Collider)> collidersToAdd = new Queue<(int, int, Collider)>();

        public Map(int width, int height)
        {
            WidthInTiles = width;
            HeightInTiles = height;

            Layers = new TileLayer[2];
            Layers[0] = new TileLayer(width, height, 0, 1f) { Map = this };
            Layers[1] = new TileLayer(width, height, 1, 0.4f) { Map = this };

            colliders = new Collider[width * height];
        }

        public void PlaceAllColliders()
        {
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    var pos = new Point(x, y);
                    if(ShouldHaveCollider(GetTile(x, y, 0)))
                    {
                        var coll = new BoxCollider(Tile.SIZE, Tile.SIZE);
                        collidersToAdd.Enqueue((x, y, coll));
                    }
                }
            }

            if (!Entity.IsNullOrDestroyed())
                DequeueColliders();
        }

        private bool ShouldHaveCollider(Tile tile)
        {
            return false;
        }

        private void DequeueColliders()
        {
            while (collidersToAdd.Count > 0)
            {
                (int x, int y, Collider collider) = collidersToAdd.Dequeue();
                SetCollider(collider, x, y);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Collider GetCollider(int x, int y)
        {
            if (IsInBounds(x, y))
                return colliders[x + y * WidthInTiles];
            return null;
        }

        internal void SetCollider(Collider c, int x, int y)
        {
            var existing = GetCollider(x, y);
            if (existing != null)
            {
                Entity.RemoveComponent(existing);
                // TODO recycle.
            }
            colliders[x + y * WidthInTiles] = c;
            if (c != null)
            {
                c.LocalOffset = TileToWorldPosition(new Point(x, y));
                Entity.AddComponent(c);
            }
        }

        private void EnsureCollider(int x, int y, bool shouldExist)
        {
            var curr = GetCollider(x, y);
            if (curr == null && shouldExist)
                SetCollider(new BoxCollider(Tile.SIZE, Tile.SIZE), x, y); // TODO get from pool
            else if (curr != null && !shouldExist)
                SetCollider(null, x, y);
        }

        public Tile SetTile(int x, int y, int z, TileDef tile, bool fromLoadOrUnload = false)
        {
            if (z < 0 || z >= Layers.Length)
            {
                Debug.Error($"Cannot place tile at {x}, {y}, {z}: {z} is not a valid layer index.");
                return null;
            }

            var created = Layers[z].SetTile(x, y, tile, fromLoadOrUnload);

            #region Update colliders
            if (!fromLoadOrUnload && z == 0)
            {
                // Self.
                EnsureCollider(x, y, ShouldHaveCollider(created));

                EnsureCollider(x - 1, y, ShouldHaveCollider(GetTile(x - 1, y, 0)));
                EnsureCollider(x - 1, y - 1, ShouldHaveCollider(GetTile(x - 1, y - 1, 0)));
                EnsureCollider(x, y - 1, ShouldHaveCollider(GetTile(x, y - 1, 0)));
                EnsureCollider(x + 1, y - 1, ShouldHaveCollider(GetTile(x + 1, y - 1, 0)));
                EnsureCollider(x + 1, y, ShouldHaveCollider(GetTile(x + 1, y, 0)));
                EnsureCollider(x + 1, y + 1, ShouldHaveCollider(GetTile(x + 1, y + 1, 0)));
                EnsureCollider(x, y + 1, ShouldHaveCollider(GetTile(x, y + 1, 0)));
                EnsureCollider(x - 1, y + 1, ShouldHaveCollider(GetTile(x - 1, y + 1, 0)));
            }
            #endregion

            return created;
        }

        public Tile GetTile(int x, int y, int z)
        {
            if (z < 0 || z >= Layers.Length)
            {
                Debug.Error($"Cannot get tile at {x}, {y}, {z}: {z} is not a valid layer index.");
                return null;
            }

            return Layers[z].GetTile(x, y);
        }

        public void AutoSlope(int x, int y, int z)
        {
            var tile = GetTile(x, y, z);
            if (tile == null)
                return;

            bool left = tile.AutoSlopeWith(GetTile(x - 1, y, z));
            bool right = tile.AutoSlopeWith(GetTile(x + 1, y, z));
            bool up = tile.AutoSlopeWith(GetTile(x, y - 1, z));
            bool down = tile.AutoSlopeWith(GetTile(x, y + 1, z));

            byte slope = 0;
            if (left && up)
                slope = 1;
            else if (up && right)
                slope = 2;
            else if (right && down)
                slope = 3;
            else if (down && left)
                slope = 4;

            tile.SlopeIndex = slope;
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

            tileShaderMaskParam = GameScene.TilesShader.Parameters["masks"];
            tileShaderMatrixParam = GameScene.TilesShader.Parameters["_viewProjectionMatrix"];
            DequeueColliders();
            Current = this;
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            tileShaderMaskParam = null;
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
        public bool CanStandAt(int x, int y)
        {
            Tile above = GetTile(x, y - 1, 0);
            Tile at = GetTile(x, y, 0);
            Tile below = GetTile(x, y + 1, 0);

            bool basic = ((below != null && below.CanStandOn) || (at != null && at.CanStandIn)) && (at == null || at.CanStandIn) && (above == null || above.CanStandIn);
            if (basic)
                return true;

            // Check for a double slope situaition.
            if (below == null || above == null)
                return false;
            if (below.SlopeIndex == 3 && above.SlopeIndex == 1)
                return true;
            if (below.SlopeIndex == 4 && above.SlopeIndex == 2)
                return true;

            return false;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            Tile.StartDraw(tileShaderMaskParam, tileShaderMatrixParam);

            for (int i = Layers.Length - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                layer.Render(batcher, camera, (float)i / Layers.Length);
            }

            //batcher.DrawHollowRect(new Rectangle(MouseTileCoordinates.X * Tile.SIZE - Tile.SIZE / 2, MouseTileCoordinates.Y * Tile.SIZE - Tile.SIZE / 2, Tile.SIZE, Tile.SIZE), Color.Red, 1);
        }
    }
}
