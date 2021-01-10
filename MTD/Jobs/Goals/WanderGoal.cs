using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MTD.Effects;
using MTD.Jobs.Tasks;
using MTD.World;
using Nez;

namespace MTD.Jobs.Goals
{
    public class WanderGoal : Goal
    {
        private static List<Point> tempPoints = new List<Point>();

        public override string Name { get; } = "Wandering";

        public override void Plan()
        {
            // Find a position to wander to.
            var pos = FindWanderPos(7, 3);
            if (pos == null)
            {
                // Do not add any tasks, will flag as completed.
                return;
            }

            if(Random.Chance(0.3f))
                AddTask(new MoteTask(MoteDef.Get("SuspenseMote")));

            AddTask(new WaitTask(Random.Range(0.5f, 1f)));
            AddTask(new MoveToTask(pos.Value));
        }

        private Point? FindWanderPos(int xRadius, int yRadius)
        {
            var cp = Pawn.CurrentTilePos;
            int sx = cp.X - xRadius;
            int ex = cp.X + xRadius;
            int sy = cp.Y - yRadius;
            int ey = cp.Y + yRadius;
            var map = Map.Current;

            for (int x = sx; x <= ex; x++)
            {
                for (int y = sy; y <= ey; y++)
                {
                    if (x == cp.X && y == cp.Y)
                        continue;

                    if (map.CanStandAt(x, y))
                    {
                        tempPoints.Add(new Point(x, y));
                    }
                }
            }

            if (tempPoints.Count == 0)
                return null;

            var pos = tempPoints.GetRandom();
            return pos;
        }
    }
}
