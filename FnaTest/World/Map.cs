using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Nez;
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

        private Collider[] colliders;
        private Queue<(int, int, Collider)> collidersToAdd = new Queue<(int, int, Collider)>();

        public Map(int width, int height)
        {
            WidthInTiles = width;
            HeightInTiles = height;

            Layers = new TileLayer[1];
            Layers[0] = new TileLayer(width, height, 0) { Map = this };

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
            if (tile == null || !tile.IsSolid)
                return false;

            int x = tile.X;
            int y = tile.Y;
            int d = tile.Layer.Depth;

            var other = GetTile(x - 1, y, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x - 1, y - 1, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x, y - 1, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x + 1, y - 1, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x + 1, y, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x + 1, y + 1, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x, y + 1, d);
            if (other == null || !other.IsSolid)
                return true;

            other = GetTile(x - 1, y + 1, d);
            if (other == null || !other.IsSolid)
                return true;

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

            DequeueColliders();
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

        /// <summary>
        /// Can a pawn stand on top of the tile at this position?
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanStand(int ex, int ey)
        {
            if (!IsInBounds(ex, ey))
                return false;

            var tile = Layers[0].GetTileFast(ex, ey);
            if (tile != null && !tile.Def.CanClimb)
                return false; // Cannot walk into a solid tile that is not climbable.

            if (tile == null)
            {
                if (!IsInBounds(ex, ey + 1))
                    return false;
                var below = Layers[0].GetTileFast(ex, ey + 1);
                if (below == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Could a pawn walk through the tile at this position?
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsImpassable(int x, int y)
        {
            if (!IsInBounds(x, y))
                return true;

            var tile = Layers[0].GetTileFast(x, y);
            if (tile != null && !tile.Def.CanClimb)
                return true;

            return false;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                var layer = Layers[i];
                layer.Render(batcher, camera, (float)i / Layers.Length);
            }

            //batcher.DrawHollowRect(new Rectangle(MouseTileCoordinates.X * Tile.SIZE - Tile.SIZE / 2, MouseTileCoordinates.Y * Tile.SIZE - Tile.SIZE / 2, Tile.SIZE, Tile.SIZE), Color.Red, 1);
        }
    }
}
