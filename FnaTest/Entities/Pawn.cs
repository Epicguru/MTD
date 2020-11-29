using MTD.Components;
using Nez;

namespace MTD.Entities
{
    public abstract class Pawn : Component, IUpdatable
    {
        public Health Health { get; internal set; }
        /// <summary>
        /// The time, in (scaled) seconds, since this pawn took damage.
        /// Does not persist after save or load.
        /// </summary>
        public float TimeSinceHit { get; private set; }
        public readonly PawnDef Def;

        public virtual bool DoHurtEffect
        {
            get
            {
                return Def.DoHurtEffect;
            }
        }

        protected float hurtEffectLerp;

        protected Pawn(PawnDef def)
        {
            this.Def = def;
        }

        public virtual void Update()
        {
            hurtEffectLerp -= Time.DeltaTime * 2f;
            hurtEffectLerp = Mathf.Clamp01(hurtEffectLerp);
            TimeSinceHit += Time.DeltaTime;
        }

        public virtual void UponHealthChanged(int amount, string changer)
        {
            if(DoHurtEffect && amount < 0)
                hurtEffectLerp = 1f;
            TimeSinceHit = 0f;
        }
    }
}
