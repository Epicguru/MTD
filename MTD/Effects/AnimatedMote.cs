using Microsoft.Xna.Framework;
using Nez;
using Spriter2Nez;

namespace MTD.Effects
{
    public class AnimatedMote : Mote
    {
        private NezAnimator animator;

        public Vector2 LocalOffset;
        public float Speed;
        public Color Color;

        public AnimatedMote(MoteDef def) : base(def)
        {
        }

        public override void Reset()
        {
            base.Reset();

            var animDef = Def as AnimatedMoteDef;

            if (animator == null)
            {
                // LoadAnimationContent caches the result, so this shouldn't be too slow...
                var proj = Core.Scene.Content.LoadAnimationProject(animDef.Project);
                var entity = proj?.GetEntity(animDef.Entity);
                if (entity == null)
                    return;

                animator = new NezAnimator(entity);
            }

            this.Speed = animDef.Speed;
            this.LocalOffset = animDef.LocalOffset;
            this.Color = animDef.Color;

            if (!string.IsNullOrWhiteSpace(animDef.Animation))
                animator.Play(animDef.Animation);
        }

        public override bool Draw(Batcher batcher, Camera camera, float depth)
        {
            if (animator == null) // Caused when animator failed to load.
                return false;
            if (TimeSinceMaintain > Def.FadeEndTime) // Out of time!
                return false;

            animator.Scale = base.Scale;
            animator.Rotation = base.Rotation;
            animator.Position = base.Position + LocalOffset;
            animator.Speed = this.Speed;
            animator.Color = this.Color * (1f - base.FadePercentage);

            float dt = (Def as AnimatedMoteDef).UseUnscaledDeltaTime ? Time.UnscaledDeltaTime : Time.DeltaTime;
            animator.Update(dt * 1000f);
            animator.Draw(batcher, camera);

            return true;
        }
    }

    public class AnimatedMoteDef : MoteDef
    {
        public string Project; // Such as 'Animations/MyProject'
        public string Entity; // Such as 'Character'
        public string Animation; // Such as 'Run'

        public Vector2 LocalOffset;
        public bool UseUnscaledDeltaTime;
        public float Speed;
        public Color Color;

        public override Mote Create()
        {
            return new AnimatedMote(this);
        }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Project))
            {
                ValidateError("Null or blank Project path!");
            }
            if (string.IsNullOrWhiteSpace(Entity))
            {
                ValidateError("Null or blank Entity name!");
            }
        }
    }
}
