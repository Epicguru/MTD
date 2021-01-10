using Microsoft.Xna.Framework;
using MTD.Effects;

namespace MTD.Jobs.Tasks
{
    public class MoteTask : TaskWithStart
    {
        public static readonly Vector2 DefaultPawnOffset = new Vector2(0f, -96);

        public override string Name { get; } = "Display mote";

        private readonly MoteDef mote;
        private readonly Vector2 motePos;

        public MoteTask(MoteDef mote, Vector2? pawnOffset = null)
        {
            // Null mote? Whatever.
            // Could also Fail() but it seems silly to fail a goal just because
            // a mote wasn't displayed.
            if (mote == null)
                Complete();

            this.mote = mote;
            this.motePos = pawnOffset ?? DefaultPawnOffset;
            Name = $"Display mote ({mote?.DefName ?? "null"})";
        }

        public override void OnStart()
        {
            if (mote != null)
            {
                Vector2 pos = Pawn.Transform.Position + motePos;
                Mote.Spawn(mote, pos);
                // The mote is not maintained.
            }
            Complete();
        }
    }
}
