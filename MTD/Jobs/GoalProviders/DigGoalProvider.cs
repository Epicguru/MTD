using System.Linq;
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

            Map map = Map.Current;
            if (map.DigPoints.Count == 0)
                return null;

            var list = map.DigPoints.ToList();
            list.Sort((a, b) =>
            {
                int adx = a.X - currentPos.X;
                int ady = a.Y - currentPos.Y;
                int bdx = b.X - currentPos.X;
                int bdy = b.Y - currentPos.Y;
                int aDst = adx * adx + ady * ady;
                int bDst = bdx * bdx + bdy * bdy;

                return aDst - bDst;
            });

            return new RemoveTileGoal(list);
        }
    }
}
