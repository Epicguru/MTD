using Microsoft.Xna.Framework;
using MTD.Jobs.Tasks;

namespace MTD.Jobs.Goals
{
    /// <summary>
    /// Very simple goal that instructs the pawn to move to a target position.
    /// Basically just uses a single <see cref="MoveToTask"/>.
    /// </summary>
    public class MoveToGoal : Goal
    {
        public override string Name => $"Move To {TargetPosition}";
        public Point TargetPosition { get; }

        public MoveToGoal(Point targetPos)
        {
            this.TargetPosition = targetPos;
        }

        public override void Plan()
        {
            AddTask(new MoveToTask(TargetPosition));
        }
    }
}
