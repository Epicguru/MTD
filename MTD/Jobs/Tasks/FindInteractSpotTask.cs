using Microsoft.Xna.Framework;
using MTD.World.Pathfinding;
using Nez;
using System.Collections.Generic;

namespace MTD.Jobs.Tasks
{
    public class FindInteractSpotTask : TaskWithStart
    {
        public override string Name { get; }

        private readonly Point targetPos;
        private int pending;
        private readonly bool allowBelow;
        private List<Point>[] paths;
        private int index;

        public FindInteractSpotTask(Point targetTile, bool allowBelow = true)
        {
            /*
             * # - Valid spot
             * x - Target spot
             * 0 - invalid spot
             *
             * Valid interact positions:
             * ###
             * #x#
             * #0#
             * ###
             */

            this.targetPos = targetTile;
            this.allowBelow = allowBelow;
            Name = $"Finding interaction spot for {targetPos}";
        }

        public override void OnStart()
        {
            if (!(Goal is IPathCallback))
            {
                Debug.Error($"When using FindInteractSpotTask, the Goal '{Goal.GetType().Name}' should implement IPathCallback. Task will now fail.");
                Fail();
                return;
            }

            pending = allowBelow ? 10 : 7;
            paths = new List<Point>[pending];
            Point start = Pawn.CurrentTilePos;
            Point end = targetPos;
            void Find(int x, int y)
            {
                Main.Pathfinder.FindPath(start, new Point(x, y), UponPathFound, new Point(x, y));
            }

            Find(end.X - 1, end.Y - 1);
            Find(end.X + 0, end.Y - 1);
            Find(end.X + 1, end.Y - 1);

            Find(end.X - 1, end.Y);
            Find(end.X + 1, end.Y);

            Find(end.X - 1, end.Y + 1);
            Find(end.X + 1, end.Y + 1);

            if (allowBelow)
            {
                Find(end.X - 1, end.Y + 2);
                Find(end.X + 0, end.Y + 2);
                Find(end.X + 1, end.Y + 2);
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (pending != 0)
                return;

            if(index == 0)
            {
                // Not a single path was found.
                Fail();
                return;
            }

            if (index == 1)
            {
                // Just one path was found. Use it.
                SendPath(0);
                paths = null;
                Complete();
                return;
            }

            // More than one possible path - find the shortest.
            // TODO consider diagonals.
            int bestLen = int.MaxValue;
            int selected = -1;
            for (int i = 0; i < index; i++)
            {
                var path = paths[i];
                if (path.Count < bestLen)
                {
                    bestLen = path.Count;
                    selected = i;
                }
            }
            SendPath(selected);
            paths = null;
            Complete();
        }

        private void UponPathFound(PathResult result, List<Point> points, object target)
        {
            pending--;
            if (result != PathResult.SUCCESS)
            {
                // START_IS_END means that the pawn is already in the interaction spot. Nice.
                if (result == PathResult.START_IS_END)
                {
                    paths[index++] = new List<Point> {(Point) target};
                }
                return;
            }

            paths[index++] = points;
        }

        private void SendPath(int index)
        {
            // Send the path at the index, free up all other path lists.

            var pc = (Goal as IPathCallback);

            List<Point> path = null;
            for (int i = 0; i < paths.Length; i++)
            {
                if (i == index)
                {
                    path = paths[i];
                }
                else
                {
                    Pathfinder.FreePath(paths[i]);
                }
            }

            pc.OnPathFound(path);
        }
    }

    public interface IPathCallback
    {
        public void OnPathFound(List<Point> points);
    }
}
