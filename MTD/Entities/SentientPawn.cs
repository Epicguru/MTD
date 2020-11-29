using MTD.Jobs;

namespace MTD.Entities
{
    public abstract class SentientPawn : Pawn
    {
        public readonly JobManager JobManager;

        protected SentientPawn(PawnDef def) : base(def)
        {
            JobManager = CreateJobManager();
        }

        protected virtual JobManager CreateJobManager()
        {
            return new JobManager(this);
        }
    }
}
