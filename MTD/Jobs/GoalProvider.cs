using System;
using MTD.Entities;

namespace MTD.Jobs
{
    public abstract class GoalProvider : IComparable<GoalProvider>
    {
        public readonly GoalProviderDef Def;
        public JobManager Manager { get; internal set; }
        public SentientPawn Pawn { get { return Manager?.Pawn; } }
        public abstract string Name { get; }

        protected GoalProvider(GoalProviderDef def)
        {
            this.Def = def;
        }

        /// <summary>
        /// Should return a value in the range -1 to 1.
        /// Higher priorities get picked first.
        /// Zero is neutral, -1 is lowest priority, 1 is highest priority.
        /// </summary>
        public abstract float GetPriority();

        /// <summary>
        /// Returns a new goal.
        /// </summary>
        public abstract Goal CreateGoal();

        public virtual int CompareTo(GoalProvider other)
        {
            float selfP = GetPriority();
            float otherP = other.GetPriority();

            if (selfP > otherP)
                return -1;
            if (selfP < otherP)
                return 1;
            return 0;
        }
    }
}
