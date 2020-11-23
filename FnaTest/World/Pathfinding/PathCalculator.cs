
#define DO_DIAGONALS

using Microsoft.Xna.Framework;
using Priority_Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nez;

namespace MTD.World.Pathfinding
{
    public class PathCalculator : IDisposable
    {
        private static readonly ConcurrentQueue<List<Point>> pathCache = new ConcurrentQueue<List<Point>>();
        public static void FreePath(List<Point> path)
        {
            if (path == null)
                return;

            path.Clear();
            pathCache.Enqueue(path);
        }
        private static List<Point> CreatePath()
        {
            if (pathCache.TryDequeue(out var list))
                return list;

            return new List<Point>(128);
        }

        public readonly int MaxOpenNodes;
        public int PreviousPathPNodeUsage { get; private set; }
        public int PreviousPoolExcess { get; private set; }
        public int PNodeMaxPoolCapacity
        {
            get
            {
                return pool?.Capacity ?? 1;
            }
        }
        public int LastPeakOpenNodes { get; private set; }

        public Map Map;

        private FastPriorityQueue<PNode> open;
        private Dictionary<PNode, PNode> cameFrom;
        private Dictionary<PNode, float> costSoFar;
        private List<PNode> near;
        private PNodePool pool;

        public PathCalculator(Map map, int maxOpenNodes = 2000)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            MaxOpenNodes = maxOpenNodes;

            open = new FastPriorityQueue<PNode>(maxOpenNodes);
            cameFrom = new Dictionary<PNode, PNode>();
            costSoFar = new Dictionary<PNode, float>();
            near = new List<PNode>();
            pool = new PNodePool(map.WidthInTiles * map.HeightInTiles * 5); // Would theoretically require 4 to never have to allocate.
        }

        public void Dispose()
        {
            if (open == null)
                return; // Already disposed.

            Clear();
            open = null;
            cameFrom = null;
            costSoFar = null;
            near = null;
            pool.Dispose();
            pool = null;
        }

        public PathResult Calculate(Point start, Point end, out List<Point> path)
        {
            path = null;
            if (start == end)
            {
                return PathResult.ERROR_START_IS_END;
            }
            if (!Map.CanStandAt(end.X, end.Y))
            {
                return PathResult.ERROR_END_IS_UNWALKABLE;
            }

            Clear(); // This is a pretty expensive call... Maybe find a clever way to optimize?
            PreviousPathPNodeUsage = pool.UsedNodeCount;
            PreviousPoolExcess = pool.PoolExcess;
            LastPeakOpenNodes = 0;
            pool.Restart();

            var startNode = pool.Create(start);
            var endNode = pool.Create(end);

            open.Enqueue(startNode, 0f);
            cameFrom[startNode] = startNode;
            costSoFar[startNode] = 0f;

            int count = 1; // Keep custom count to avoid the log(n) complexity of open.Count
            while (count > 0)
            {
                if (count >= MaxOpenNodes - 8)
                {
                    return PathResult.ERROR_PATH_TOO_LONG;
                }

                //if (pool.SpaceRemaining <= 8)
                //{
                //    return PathResult.ERROR_PATH_TOO_LONG;
                //}

                if (count > LastPeakOpenNodes)
                    LastPeakOpenNodes = count;

                PNode current = open.Dequeue();
                count--;

                if (current.Equals(endNode))
                {
                    path = TracePath(endNode);
                    return PathResult.SUCCESS;
                }

                float currentCostSoFar = costSoFar[current];

                Vector2 pos = Map.Current.TileToWorldPosition(current);
                //Debug.DrawHollowBox(pos, Tile.SIZE, Color.Orange, 0);

                GetNear(current);
                foreach (var n in near)
                {
                    // newCost: This is the (exact) total path cost from the start node to this neighbor.
                    float newCost = currentCostSoFar + GetCost(current, n);
                    bool hasCostSoFar = costSoFar.TryGetValue(n, out float nCostSoFar);

                    Debug.DrawHollowBox(pos, Tile.SIZE - 10, Color.Blue);

                    // If the node has never been explored, or the current path is shorter than a prevous route...
                    if (!hasCostSoFar || newCost < nCostSoFar)
                    {
                        costSoFar[n] = newCost;
                        float priority = newCost + Heuristic(n, endNode);
                        open.Enqueue(n, priority);
                        count++;
                        cameFrom[n] = current;
                        pos = Map.Current.TileToWorldPosition(n);
                        //Debug.DrawHollowBox(pos, Tile.SIZE - 4, Color.Blue, 0);
                    }
                }
            }

            return PathResult.ERROR_PATH_NOT_FOUND;
        }

        private List<Point> TracePath(PNode end)
        {
            var path = CreatePath();

            PNode child = end;

            while (true)
            {
                PNode previous = cameFrom[child];
                path.Add(child);
                if (!previous.Equals(child))
                {
                    child = previous;
                }
                else
                {
                    break;
                }
            }

            path.Reverse();
            return path;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetNear(PNode node)
        {
            near.Clear();

            int x = node.X;
            int y = node.Y;
            const int z = 0;

            // Need to return a list of all tiles that can be moved to from the current tile.

            Tile tSelf = Map.GetTile(x, y, z);

            Tile tLeft = Map.GetTile(x - 1, y, z);
            Tile tTopLeft = Map.GetTile(x - 1, y - 1, z);
            Tile tTopTopLeft = Map.GetTile(x - 1, y - 2, z);
            Tile tBotLeft = Map.GetTile(x - 1, y + 1, z);
            Tile tBotBotLeft = Map.GetTile(x - 1, y + 2, z);

            Tile tRight = Map.GetTile(x + 1, y, z);
            Tile tTopRight = Map.GetTile(x + 1, y - 1, z);
            Tile tTopTopRight = Map.GetTile(x + 1, y - 2, z);
            Tile tBotRight = Map.GetTile(x + 1, y + 1, z);
            Tile tBotBotRight = Map.GetTile(x + 1, y + 2, z);

            Tile tTop = Map.GetTile(x, y - 1, z);
            Tile tTopTop = Map.GetTile(x, y - 2, z);

            Tile tBot = Map.GetTile(x, y + 1, z);
            Tile tBotBot = Map.GetTile(x, y + 2, z);

            // Left.
            // Can move left if both left and top-left tiles are clear, and there is something to stand on below.
            if (CanStandOn(tBotLeft) && CanStandIn(tLeft) && CanStandIn(tTopLeft))
                near.Add(pool.Create(x - 1, y));

            // Right.
            if (CanStandOn(tBotRight) && CanStandIn(tRight) && CanStandIn(tTopRight))
                near.Add(pool.Create(x + 1, y));

            // Straight up.
            if (CanStandIn(tTopTop) && (CanClimb(tTop) || CanClimb(tSelf)))
                near.Add(pool.Create(x, y - 1));

            // Straight down.
            if (CanClimb(tBot) || (CanStandOn(tBotBot) && CanStandIn(tBot)))
                near.Add(pool.Create(x, y + 1));

            // Diagonal - bottom left & right.
            // Can move down-left if that spot can be stood on.
            if(CanStandIn(tBotLeft) && CanStandIn(tLeft) && CanStandIn(tTopLeft) && (CanStandOn(tBotBotLeft) || CanStandIn(tBotLeft, true)))
                near.Add(pool.Create(x - 1, y + 1));

            if (CanStandIn(tBotRight) && CanStandIn(tRight) && CanStandIn(tTopRight) && (CanStandOn(tBotBotRight) || CanStandIn(tBotRight, true)))
                near.Add(pool.Create(x + 1, y + 1));

            // Diagonal - top left & right.
            if (CanStandIn(tTopRight) && CanStandIn(tTopTopRight) && CanStandOn(tRight) && CanStandIn(tTopTop))
                near.Add(pool.Create(x + 1, y - 1));

            if (CanStandIn(tTopLeft) && CanStandIn(tTopTopLeft) && CanStandOn(tLeft) && CanStandIn(tTopTop))
                near.Add(pool.Create(x - 1, y - 1));
        }

        /// <summary>
        /// Can a pawn stand on top of this tile?
        /// Air, ladder etc. will return false. Solid surfaces will return true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanStandOn(Tile tile) => tile != null && tile.CanStandOn;

        /// <summary>
        /// Can a pawn stand with it's body occupying this tile?
        /// Air, ladder etc. will return true. A solid wall will return false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanStandIn(Tile tile, bool notAir = false) => notAir ? tile != null && tile.CanStandIn : tile == null || tile.CanStandIn;

        /// <summary>
        /// Can this tile be climbed? Ladders etc. can be climbed.
        /// </summary>
        /// <param name="tile"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanClimb(Tile tile) => tile != null && tile.Def.CanClimb;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Abs(int x)
        {
            if (x < 0)
                return -x;
            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetCost(PNode a, PNode b)
        {
            const float DIAGONAL_DST = 1.41421356237f;

            // Only intended for neighbours.

            // Is directly horzontal
            if (Abs(a.X - b.X) == 1 && a.Y == b.Y)
            {
                return 1;
            }

            // Directly vertical.
            if (Abs(a.Y - b.Y) == 1 && a.X == b.X)
            {
                return 1;
            }

            // Assume that it is on one of the corners.
            return DIAGONAL_DST;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float Heuristic(PNode a, PNode b)
        {
            // Gives a rough distance.
            return Abs(a.X - b.X) + Abs(a.Y - b.Y);
            //return Mathf.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        private void Clear()
        {
            open.Clear();
            near.Clear();
            cameFrom.Clear();
            costSoFar.Clear();
        }
    }
}
