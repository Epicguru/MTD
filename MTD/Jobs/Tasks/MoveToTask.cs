using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MTD.World;
using MTD.World.Pathfinding;
using Nez;

namespace MTD.Jobs.Tasks
{
    /// <summary>
    /// Tries to move the pawn to a destination position.
    /// Will fail if path cannot be found, or if the path walking
    /// is interrupted.
    /// </summary>
    public class MoveToTask : TaskWithStart
    {
        public override string Name { get { return _name; } }
        public Point StartPos { get; private set; }
        public Point? EndPos { get; private set; }
        public List<Point> Points;

        private string _name;

        public MoveToTask(Point? targetPos)
        {
            if (targetPos != null)
            {
                bool canStand = Map.Current.CanStandAt(targetPos.Value.X, targetPos.Value.Y);

                // Can't stand at the target position? Fail immediately.
                if (!canStand)
                    Fail();
            }

            EndPos = targetPos;
            _name = $"Move to {EndPos} (waiting)";
        }

        public override void OnStart()
        {
            if (EndPos != null)
            {
                StartPos = Pawn.PathFollower.CurrentTilePos;
                Main.Pathfinder.FindPath(StartPos, EndPos.Value, UponPathFound);
                _name = $"Move to {EndPos} (looking for path)";
            }
            else if (Points == null || Points.Count < 2)
            {
                Debug.Error($"MoveToTask was created with no end position, but when the task started the Goal '{Goal.GetType().Name}'" +
                            $"had still not assigned to the Point list, or supplied a list with less than 2 points.");
                Fail();
            }
            else
            {
                EndPos = Points[Points.Count - 1];
                UponPathFound(PathResult.SUCCESS, Points, null);
            }
            
        }

        protected virtual void UponPathFound(PathResult result, List<Point> points, object _)
        {
            if (result != PathResult.SUCCESS)
            {
                if (result == PathResult.START_IS_END)
                {
                    // Nice! No need to move.
                    Complete();
                    return;
                }

                // Path not found, task fails.
                Fail();
                return;
            }

            // Path found, start walking!
            Pawn.PathFollower.SetPath(points);
            _name = $"Move to {EndPos} (walking)";
        }

        public override void Tick()
        {
            base.Tick();

            // If the pawn has reached the target position, then the task is complete.
            // TODO when the path follower has the ability to detect that
            // the path is blocked, that will have to be accounted for here.
            if (Pawn.PathFollower.CurrentTilePos == EndPos)
            {
                Complete();
            }
        }

        public override void UponInterrupt()
        {
            base.UponInterrupt();

            // Need to stop entity from walking the path.
            Pawn.PathFollower.ResetPath();
        }
    }
}
