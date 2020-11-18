
using Microsoft.Xna.Framework;
using Nez;
using Priority_Queue;
using System;

namespace MTD.World.Pathfinding
{
    public class PNodePool : IDisposable
    {
        public readonly int Capacity;

        public int SpaceRemaining
        {
            get
            {
                return Capacity - poolIndex;
            }
        }
        public int UsedNodeCount
        {
            get
            {
                return poolIndex;
            }
        }
        public int PoolExcess { get; private set; }

        private PNode[] pool;
        private int poolIndex;

        public PNodePool(int capacity)
        {
            this.Capacity = capacity;
            this.pool = new PNode[capacity];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = new PNode(0, 0);
            }
        }

        public void Restart()
        {
            poolIndex = 0;
            PoolExcess = 0;
        }

        public PNode Create(int x, int y)
        {
            if (poolIndex >= pool.Length)
            {
                PoolExcess++;
                return new PNode(x, y);
            }

            var current = pool[poolIndex];
            poolIndex++;
            current.X = x;
            current.Y = y;
            return current;
        }

        public PNode Create(Point point)
        {
            return this.Create(point.X, point.Y);
        }

        public void Dispose()
        {
            pool = null;
        }
    }

    public class PNode : FastPriorityQueueNode
    {
        public static int PoolCount
        {
            get
            {
                return 0;
            }
        }

        internal PNode(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X, Y;

        public override bool Equals(object obj)
        {
            var other = (PNode)obj;
            return this.X == other.X && this.Y == other.Y;
        }

        public override int GetHashCode()
        {
            return X + Y * 7;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static implicit operator Point(PNode node)
        {
            return new Point(node.X, node.Y);
        }
    }
}
