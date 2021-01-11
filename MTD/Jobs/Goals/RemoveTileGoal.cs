using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MTD.Jobs.Tasks;
using MTD.World;
using Nez;

namespace MTD.Jobs.Goals
{
    public class RemoveTileGoal : Goal, IPathCallback
    {
        public override string Name { get; } = "Remove tile";

        private MoveToTask moveTask;
        private RemoveTileTask removeTask;
        private readonly List<Point> points;

        public RemoveTileGoal(Point target) : this(new List<Point> {target})
        {

        }

        public RemoveTileGoal(List<Point> pointsToTry)
        {
            this.points = pointsToTry;
        }

        public override void Plan()
        {
            Task CreateFindSpotTask(Point target)
            {
                return new FindInteractSpotTask(target)
                {
                    OnFail = _ =>
                    {
                        if (points.Count == 0)
                        {
                            Debug.Trace("Failed all, dig failed.");
                            return; // Leave the task as failed, so the goal will fail.
                        }

                        Debug.Trace("Failed first, moving on to next...");
                        Point next = points[0];
                        points.RemoveAt(0);
                        ReplaceCurrent(CreateFindSpotTask(next));

                        // Update remove tile task. Move to task is updated automatically upon finding a path.
                        removeTask.TargetTile = GetTile(next);
                    }
                };
            }

            Tile GetTile(Point target)
            {
                const int z = 0;
                return Map.Current.GetTile(target.X, target.Y, z);
            }

            Point first = points[0];
            points.RemoveAt(0);

            AddTask(CreateFindSpotTask(first));
            AddTask(moveTask = new MoveToTask(null));
            AddTask(removeTask = new RemoveTileTask(GetTile(first)));
        }

        public void OnPathFound(List<Point> points)
        {
            if (points.Count == 1)
            {
                // This is what happens when start == end point.
                // Remove the move task, no need to move at all.
                RemoveTask(moveTask);
                moveTask = null;
                return;
            }

            // Found a real path to move down, set points.
            moveTask.Points = points;
        }
    }
}
