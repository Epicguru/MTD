using MTD.Entities;

namespace MTD.Jobs
{
    public class JobManager
    {
        public readonly SentientPawn Pawn;
        public Job CurrentJob { get; private set; }

        public JobManager(SentientPawn pawn)
        {
            Pawn = pawn ?? throw new System.ArgumentNullException(nameof(pawn));
        }

        public void Tick()
        {
            if (CurrentJob == null)
            {
                CurrentJob = FindNewJob();
            }

            if (CurrentJob != null)
            {
                CurrentJob.Manager = this;
                bool canContinue = CurrentJob.TickInternal(out bool _);
                if (!canContinue)
                    CurrentJob = null;
            }
        }

        public void Interrupt(Job replacementJob)
        {
            if (CurrentJob == null)
            {
                CurrentJob = replacementJob;
            }
            else
            {
                var toInterrupt = CurrentJob.DeepestStarted;
                toInterrupt?.SendInterruptUp();
                CurrentJob = replacementJob;
            }
        }

        public virtual Job FindNewJob()
        {
            return null;
        }
    }
}
