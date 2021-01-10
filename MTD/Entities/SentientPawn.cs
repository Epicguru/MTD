using Microsoft.Xna.Framework;
using MTD.Components;
using MTD.Jobs;

namespace MTD.Entities
{
    public class SentientPawn : Pawn
    {
        public readonly JobManager JobManager;
        public PathFollower PathFollower { get; internal set; }
        public Point CurrentTilePos { get { return PathFollower.CurrentTilePos; } }

        public SentientPawn(PawnDef def) : base(def)
        {
            JobManager = CreateJobManager();
        }

        public override void Update()
        {
            base.Update();

            JobManager.Tick();
        }

        protected virtual JobManager CreateJobManager()
        {
            return new JobManager(this);
        }
    }
}
