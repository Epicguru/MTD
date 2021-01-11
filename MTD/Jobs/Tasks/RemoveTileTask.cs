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
        public Tile TargetTile
        {
            get
            {
                return _tile;
            }
            set
            {
                if (_tile == value)
                    return;

                _tile = value;
                if (_tile == null)
                {
                    _name = $"Remove tile <null>";
                    return;
                }
                
                z = _tile.Layer.Depth;
                _name = $"Remove tile {_tile}";
            }
        }

        public override string Name => _name;

        private string _name;
        private Tile _tile;
        private int z;
        private float hpSum;
        private Mote mote;

        public RemoveTileTask(Tile targetTile)
        {
            this.TargetTile = targetTile;
        }

        public override void Tick()
        {
            var t = TargetTile;
            if (t == null)
            {
                Debug.Error("RemoveTileTask started ticking, but target tile is still null. Task will now fail.");
                Fail();
                return;
            }

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
