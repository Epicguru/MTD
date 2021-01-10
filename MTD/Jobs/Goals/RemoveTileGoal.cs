using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MTD.Jobs.Tasks;
using MTD.World;

namespace MTD.Jobs.Goals
{
    public class RemoveTileGoal : Goal, IPathCallback
    {
        public override string Name { get; } = "Remove tile";

        private readonly (int x, int y, int z) tp;
        private MoveToTask moveTask;

        public RemoveTileGoal(int x, int y, int z)
        {
            tp = (x, y, z);
        }

        public override void Plan()
        {
            var tile = Map.Current.GetTile(tp.x, tp.y, tp.z);
            if (tile == null || tile.Hitpoints <= 0)
            {
                return; // Fail.
            }

            AddTask(new FindInteractSpotTask(new Point(tp.x, tp.y)));
            AddTask(moveTask = new MoveToTask(null));
            AddTask(new RemoveTileTask(tile));
        }

        public void OnPathFound(List<Point> points)
        {
            moveTask.Points = points;
        }
    }
}
