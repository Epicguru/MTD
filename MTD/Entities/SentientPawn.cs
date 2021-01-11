using Microsoft.Xna.Framework;
using MTD.Components;
using MTD.Jobs;
using MTD.World;
using Nez;

namespace MTD.Entities
{
    public class SentientPawn : Pawn
    {
        public readonly JobManager JobManager;
        public PathFollower PathFollower { get; internal set; }
        public Point CurrentTilePos { get { return PathFollower.CurrentTilePos; } }
        public bool IsFalling { get; protected set; }

        public SentientPawn(PawnDef def) : base(def)
        {
            JobManager = CreateJobManager();
        }

        public override void Update()
        {
            base.Update();

            var currentTilePos = Map.Current.WorldToTileCoordinates(Entity.Position);
            IsFalling = !Map.Current.CanStandAt(currentTilePos.X, currentTilePos.Y);
            PathFollower.Enabled = !IsFalling;
            if (IsFalling)
            {
                PathFollower.ResetPath();
                Entity.Position += new Vector2(0, Tile.SIZE * 2f * Time.DeltaTime);
                // TODO prevent falling through tiles if falling very fast (check every tile on the way down)
            }

            if (!IsFalling)
                JobManager.Tick();
            else
                JobManager.InterruptCurrent();
        }

        protected virtual JobManager CreateJobManager()
        {
            return new JobManager(this);
        }
    }
}
