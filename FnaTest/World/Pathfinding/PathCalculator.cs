using Microsoft.Xna.Framework;
using System;
using Priority_Queue;
using System.Collections.Generic;
using Nez;
using System.Runtime.CompilerServices;

namespace MTD.World.Pathfinding
{
    public class PathCalculator : IDisposable
    {
        public readonly int MaxOpenNodes;

        public Map Map;

        private FastPriorityQueue<PNode> open;
        private Dictionary<PNode, PNode> cameFrom;
        private Dictionary<PNode, float> costSoFar;
        private List<PNode> near;
        private bool left, right, below, above;
        private PNodePool pool;

        public PathCalculator(Map map, int maxOpenNodes = 500)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            MaxOpenNodes = maxOpenNodes;

            open = new FastPriorityQueue<PNode>(maxOpenNodes);
            cameFrom = new Dictionary<PNode, PNode>();
            costSoFar = new Dictionary<PNode, float>();
            near = new List<PNode>();
            pool = new PNodePool(maxOpenNodes * 8);
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
            if (!Map.IsWalkable(end.X, end.Y))
            {
                return PathResult.ERROR_END_IS_UNWALKABLE;
            }

            Clear(); // This is a pretty expensive call... Maybe find a clever way to optimize?
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

                if (pool.SpaceRemaining <= 8)
                {
                    return PathResult.ERROR_PATH_TOO_LONG;
                }

                PNode current = open.Dequeue();
                count--;

                if (current.Equals(endNode))
                {
                    path = TracePath(endNode);
                    return PathResult.SUCCESS;
                }

                float currentCostSoFar = costSoFar[current];

                //Vector2 pos = Map.Current.TileToWorldPosition(current);
                //Debug.DrawHollowBox(pos, Tile.SIZE, Color.Orange, 5);
                //Debug.DrawText(Graphics.Instance.BitmapFont, $"{(int)(costSoFar[current] + Heuristic(current, endNode))}", pos - new Vector2(Tile.SIZE * 0.4f), Color.Orange, 5, 0.5f);

                GetNear(current);
                foreach (var n in near)
                {
                    var nClone = n;

                    // newCost: This is the (exact) total path cost from the start node to this neighbor.
                    float newCost = currentCostSoFar + GetCost(current, nClone);
                    bool hasCostSoFar = costSoFar.TryGetValue(nClone, out float nCostSoFar);

                    // If the node has never been explored, or the current path is shorter than a prevous route...
                    if (!hasCostSoFar || newCost < nCostSoFar)
                    {
                        costSoFar[nClone] = newCost;
                        float priority = newCost + Heuristic(n, endNode);
                        open.Enqueue(n, priority);
                        count++;
                        cameFrom[nClone] = current;
                    }
                }
            }

            return PathResult.ERROR_PATH_NOT_FOUND;
        }

        private List<Point> TracePath(PNode end)
        {
            var path = ListPool<Point>.Obtain();

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

            // Left
            left = false;
            if (Map.IsWalkable(node.X - 1, node.Y))
            {
                near.Add(pool.Create(node.X - 1, node.Y));
                left = true;
            }

            // Right
            right = false;
            if (Map.IsWalkable(node.X + 1, node.Y))
            {
                near.Add(pool.Create(node.X + 1, node.Y));
                right = true;
            }

            // Above
            above = false;
            if (Map.IsWalkable(node.X, node.Y + 1))
            {
                near.Add(pool.Create(node.X, node.Y + 1));
                above = true;
            }

            // Below
            below = false;
            if (Map.IsWalkable(node.X, node.Y - 1))
            {
                near.Add(pool.Create(node.X, node.Y - 1));
                below = true;
            }

            // Above-Left
            if (left && above)
            {
                if (Map.IsWalkable(node.X - 1, node.Y + 1))
                {
                    near.Add(pool.Create(node.X - 1, node.Y + 1));
                }
            }

            // Above-Right
            if (right && above)
            {
                if (Map.IsWalkable(node.X + 1, node.Y + 1))
                {
                    near.Add(pool.Create(node.X + 1, node.Y + 1));
                }
            }

            // Below-Left
            if (left && below)
            {
                if (Map.IsWalkable(node.X - 1, node.Y - 1))
                {
                    near.Add(pool.Create(node.X - 1, node.Y - 1));
                }
            }

            // Below-Right
            if (right && below)
            {
                if (Map.IsWalkable(node.X + 1, node.Y - 1))
                {
                    near.Add(pool.Create(node.X + 1, node.Y - 1));
                }
            }
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
