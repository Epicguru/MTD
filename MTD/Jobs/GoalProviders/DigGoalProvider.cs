using Microsoft.Xna.Framework;
using MTD.Jobs.Goals;
using MTD.World;

namespace MTD.Jobs.GoalProviders
{
    public class DigGoalProvider : GoalProvider
    {
        public override string Name { get; } = "Dig";

        public DigGoalProvider(GoalProviderDef def) : base(def)
        {
        }

        public override float GetPriority()
        {
            return 1f;
        }

        public override Goal CreateGoal()
        {
            // Find closest tile.
            Point currentPos = Pawn.CurrentTilePos;

            Point closest = default;
            float closestDst = float.MaxValue;
            Map map = Map.Current;
            if (map.DigPoints.Count == 0)
                return null;

            foreach (var point in map.DigPoints)
            {
                int dx = point.X - currentPos.X;
                int dy = point.Y - currentPos.Y;
                float dst = dx * dx + dy * dy;

                if (dst < closestDst)
                {
                    closestDst = dst;
                    closest = point;
                }
            }

            return new RemoveTileGoal(closest.X, closest.Y, 0);
        }
    }
}
