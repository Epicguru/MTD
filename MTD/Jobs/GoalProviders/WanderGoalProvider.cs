using MTD.Jobs.Goals;

namespace MTD.Jobs.GoalProviders
{
    public class WanderGoalProvider : GoalProvider
    {
        public override string Name { get; } = "Wander";

        public WanderGoalProvider(GoalProviderDef def) : base(def)
        {

        }

        public override float GetPriority()
        {
            // Always the last thing to do.
            return -1f;
        }

        public override Goal CreateGoal()
        {
            return new WanderGoal();
        }
    }
}
