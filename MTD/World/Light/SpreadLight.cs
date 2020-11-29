using Microsoft.Xna.Framework;
using Nez;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MTD.World.Light
{
    public class SpreadLight : Light
    {
        public Point TilePosition
        {
            get
            {
                return Map.Current.WorldToTileCoordinates(Position);
            }
            set
            {
                Position = Map.Current.TileToWorldPosition(value);
            }
        }
        public float Radius = 10;

        private List<Tile> tiles = new List<Tile>();
        private Queue<Tile> openTiles = new Queue<Tile>();
        private HashSet<int> closed = new HashSet<int>();

        public override void Recalculate()
        {
            tiles.Clear();
            openTiles.Clear();
            closed.Clear();
            
            Map map = Map.Current;
            Point pos = TilePosition;

            const int z = 0;
            Tile self = new Tile(pos.X, pos.Y, Color);
            openTiles.Enqueue(self);
            closed.Add(self.Index);

            while (openTiles.Count > 0)
            {
                var current = openTiles.Dequeue();
                tiles.Add(current);

                // Get neighbors.
                if (!BlocksLight(map.GetTile(current.X - 1, current.Y, z)))
                {
                    float dst = current.Dst + 1;
                    Tile t = new Tile(current.X - 1, current.Y, MakeColor(dst));
                    t.Dst = dst;
                    if (dst <= Radius && !closed.Contains(t.Index))
                    {
                        openTiles.Enqueue(t);
                        closed.Add(t.Index);
                    }
                }
                if (!BlocksLight(map.GetTile(current.X + 1, current.Y, z)))
                {
                    float dst = current.Dst + 1;
                    Tile t = new Tile(current.X + 1, current.Y, MakeColor(dst));
                    t.Dst = dst;
                    if (dst <= Radius && !closed.Contains(t.Index))
                    {
                        openTiles.Enqueue(t);
                        closed.Add(t.Index);
                    }
                }
                if (!BlocksLight(map.GetTile(current.X, current.Y - 1, z)))
                {
                    float dst = current.Dst + 1;
                    Tile t = new Tile(current.X, current.Y - 1, MakeColor(dst));
                    t.Dst = dst;
                    if (dst <= Radius && !closed.Contains(t.Index))
                    {
                        openTiles.Enqueue(t);
                        closed.Add(t.Index);
                    }
                }
                if (!BlocksLight(map.GetTile(current.X, current.Y + 1, z)))
                {
                    float dst = current.Dst + 1;
                    Tile t = new Tile(current.X, current.Y + 1, MakeColor(dst));
                    t.Dst = dst;
                    if (dst <= Radius && !closed.Contains(t.Index))
                    {
                        openTiles.Enqueue(t);
                        closed.Add(t.Index);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool BlocksLight(World.Tile tile) => tile != null && tile.BlocksLight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Color MakeColor(float dst) => Color.Create(Color, (byte)((1f - Mathf.Clamp01(dst / Radius)) * 255));

        public override void Render(Batcher batcher, Camera camera)
        {
            if (tiles == null || tiles.Count == 0)
                return;

            foreach (var tile in tiles)
            {
                tile.Render(batcher, camera);
            }
        }
    }
}
