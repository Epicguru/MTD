using MTD.Effects;
using MTD.World;
using Nez;

namespace MTD.Jobs.Tasks
{
    /// <summary>
    /// A task for a pawn to 'dig' or 'destroy'
    /// the specified tile.
    /// This task assumes that the pawn is standing next to the tile,
    /// but does not check.
    /// The task fails if:
    /// <list type="bullet">
    /// <item>The tile is null (air)</item>
    /// <item>If the tile is destroyed before the task can complete.</item>
    /// </list>
    /// </summary>
    public class RemoveTileTask : Task
    {
        public Tile ToRemove { get; }

        public override string Name { get; }

        private readonly int z;
        private float hpSum;
        private Mote mote;

        public RemoveTileTask(Tile toRemove)
        {
            this.ToRemove = toRemove;
            if (toRemove == null)
                Fail();
            else
                z = toRemove.Layer.Depth;

            Name = $"Remove tile {toRemove}";
        }

        public override void Tick()
        {
            var t = ToRemove;
            var atPos = Map.Current.GetTile(t.X, t.Y, z);

            if (atPos == null || atPos != t)
            {
                Fail();
                mote = Mote.Spawn(MoteDef.Get("QuestionMote"), Pawn.Transform.Position + MoteTask.DefaultPawnOffset);
                return;
            }

            if (mote == null)
            {
                mote = Mote.Spawn(MoteDef.Get("AnimPickMote"), Pawn.Transform.Position + MoteTask.DefaultPawnOffset);
            }

            // How much HP is removed per second.
            float digPower = 50;

            float dt = Time.DeltaTime;
            hpSum += dt * digPower;

            int toRemove = (int)hpSum;
            if (toRemove >= 1)
            {
                hpSum -= toRemove;
                int currentHp = atPos.Hitpoints;
                atPos.SetHitpoints(currentHp - toRemove);
                if (atPos.Hitpoints <= 0)
                {
                    Complete();
                }
            }

            // Maintain that mote!
            if (mote != null)
            {
                mote.Position = Pawn.Transform.Position + MoteTask.DefaultPawnOffset;
                mote.Maintain();
            }
        }
    }
}
