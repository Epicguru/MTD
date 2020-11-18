
#define DO_DIAGONALS

using Microsoft.Xna.Framework;
using Priority_Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
            if (!Map.CanStand(end.X, end.Y))
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

            // Left
            if (Map.CanStand(x - 1, y))
            {
                near.Add(pool.Create(x - 1, y));
            }

            // Right
            if (Map.CanStand(x + 1, y))
            {
                near.Add(pool.Create(x + 1, y));
            }

            // Below
            if (Map.CanStand(x, y + 1))
            {
                near.Add(pool.Create(x, y + 1));
            }

            // Above
            if (Map.CanStand(x, y - 1))
            {
                near.Add(pool.Create(x, y - 1));
            }

#if DO_DIAGONALS

            // Below-Left. Can't have impassable tile to the left.
            if (!Map.IsImpassable(x - 1, y))
            {
                if (Map.CanStand(x - 1, y + 1))
                {
                    near.Add(pool.Create(x - 1, y + 1));
                }
            }

            // Below-Right. Can't have impassable tile to the right.
            if (!Map.IsImpassable(x + 1, y))
            {
                if (Map.CanStand(x + 1, y + 1))
                {
                    near.Add(pool.Create(x + 1, y + 1));
                }
            }

            // Above-Left. Can't have impassable tile above.
            if (!Map.IsImpassable(x, y - 1))
            {
                if (Map.CanStand(x - 1, y - 1))
                {
                    near.Add(pool.Create(x - 1, y - 1));
                }
            }

            // Above-Right. Can't have impassable tile above.
            if (!Map.IsImpassable(x, y - 1))
            {
                if (Map.CanStand(x + 1, y - 1))
                {
                    near.Add(pool.Create(x + 1, y - 1));
                }
            }
#endif
        }

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
