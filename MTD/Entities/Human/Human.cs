using Microsoft.Xna.Framework;
using Nez;

namespace MTD.Entities.Human
{
    public class Human : Pawn
    {
        public HumanRenderer Renderer { get; internal set; }
        public HumanDef HumanDef
        {
            get
            {
                return base.Def as HumanDef;
            }
        }

        public Human(HumanDef def) : base(def)
        {

        }

        public override void Update()
        {
            base.Update();

            Renderer.Color = Color.Lerp(Color.White, Color.Red, base.hurtEffectLerp);
            if (hurtEffectLerp > 0)
                Renderer.AdditionalOffset = Random.PointOnCircle() * hurtEffectLerp * 5f;
            else
                Renderer.AdditionalOffset = Vector2.Zero;

            Renderer.DrawHealthBar = TimeSinceHit < 8f;
        }
    }
}
