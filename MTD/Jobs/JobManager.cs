using System.Collections.Generic;
using MTD.Entities;
using Nez;

namespace MTD.Jobs
{
    public class JobManager
    {
        public readonly SentientPawn Pawn;
        public Goal CurrentGoal { get; private set; }

        private readonly List<GoalProvider> providers = new List<GoalProvider>();

        public JobManager(SentientPawn pawn)
        {
            this.Pawn = pawn;
        }

        public void AddProvider(GoalProvider gp)
        {
            if (gp == null || providers.Contains(gp))
                return;

            gp.Manager = this;
            providers.Add(gp);
        }

        public void Tick()
        {
            if (CurrentGoal == null)
            {
                CurrentGoal = CreateNextGoal();
                if (CurrentGoal != null)
                {
                    CurrentGoal.Manager = this;
                    CurrentGoal.Plan();
                }
                else
                    return;
            }

            if (CurrentGoal.IsInterrupted)
            {
                Debug.Trace($"Goal '{CurrentGoal}' Finished: Interrupted.");
                CurrentGoal = null;
            }
            else if (CurrentGoal.IsFailed())
            {
                Debug.Trace($"Goal '{CurrentGoal}' Finished: Failed.");
                CurrentGoal = null;
            }
            else if (CurrentGoal.IsComplete())
            {
                Debug.Trace($"Goal '{CurrentGoal}' Finished: Completed!");
                CurrentGoal = null;
            }

            CurrentGoal?.Tick();
        }

        public void InterruptCurrent(Goal newForcedGoal = null)
        {
            if(CurrentGoal != null && !CurrentGoal.IsInterrupted)
                CurrentGoal.Interrupt();

            if (newForcedGoal != null)
            {
                newForcedGoal.Manager = this;
                newForcedGoal.Plan();
            }

            CurrentGoal = newForcedGoal;
        }

        protected virtual Goal CreateNextGoal()
        {
            providers.Sort();
            foreach (var prov in providers)
            {
                var created = prov.CreateGoal();

                if (created != null)
                    return created;
            }
            return null;
        }
    }
}
