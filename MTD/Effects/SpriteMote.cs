using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Textures;

namespace MTD.Effects
{
    public class SpriteMote : Mote
    {
        public Sprite Sprite;
        public Color Color;
        public Vector2 SlideTarget;
        public float SlideTime;

        private float slideTimer;

        public SpriteMote(MoteDef def) : base(def)
        {
        }

        public override void Reset()
        {
            base.Reset();

            var sprDef = Def as SpriteMoteDef;
            Sprite = sprDef.Sprite;
            Color = sprDef.Color;
            SlideTarget = sprDef.SlideTarget;
            SlideTime = sprDef.SlideTime;

            slideTimer = 0f;
        }

        public override bool Draw(Batcher batcher, Camera camera, float depth)
        {
            if (Sprite == null)
                return false; // Null sprite? Just kill the mote.
            if (TimeSinceMaintain > Def.FadeEndTime) // Too long since maintenance happened, bye!
                return false;

            Vector2 scale = base.Scale;
            float slideLerp = SlideTime <= 0f ? 1f : Mathf.Clamp01(slideTimer / SlideTime);
            Vector2 offset = SlideTarget * slideLerp * scale;
            bool changeX = scale.X < 0f;
            bool changeY = scale.Y < 0f;
            bool fx = false;
            bool fy = false;
            var effect = SpriteEffects.None;
            if (changeX)
            {
                scale.X = -scale.X;
                fx = !fx;
            }
            if (changeY)
            {
                scale.Y = -scale.Y;
                fy = !fy;
            }
            if (changeX || changeY)
            {
                effect = fx ? (effect | SpriteEffects.FlipHorizontally) : (effect & ~SpriteEffects.FlipHorizontally);
                effect = fy ? (effect | SpriteEffects.FlipVertically) : (effect & ~SpriteEffects.FlipVertically);
            }
            Color c = Color;
            c.A = (byte)Mathf.RoundToInt(255f * (1f - base.FadePercentage));
            batcher.Draw(Sprite, Position + offset, c, base.Rotation, Sprite.Origin, scale, effect, depth);

            slideTimer += Time.UnscaledDeltaTime;
            return true;
        }
    }

    public class SpriteMoteDef : MoteDef
    {
        public Sprite Sprite;
        public Color Color;
        public Vector2 SlideTarget;
        public float SlideTime;

        public override Mote Create()
        {
            return new SpriteMote(this);
        }
    }
}
