using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Textures;
using System;
using MTD.Components;

namespace MTD.Entities.Human
{
    public class HumanRenderer : RenderableComponent
    {
		public override RectangleF Bounds
        {
            get
            {
                if (_areBoundsDirty)
                {
                    _bounds.CalculateBounds(Entity.Transform.Position, _localOffset, new Vector2(32, 64), Entity.Transform.Scale, Entity.Transform.Rotation, 64, 128);
                    _areBoundsDirty = false;
                }

                return _bounds;
            }
        }

		public Sprite BodyCol, BodyDetail, BodyOut;
        public Sprite HeadCol, Face, Hair, HeadOut;
        public Color SkinColor, HairColor;

        public Vector2 AdditionalOffset;
        public bool DrawHealthBar;

        private Health _health;

        public override void Render(Batcher batcher, Camera camera)
        {
            float r = Entity.Transform.Rotation;
            float r2 = r + (float)Math.PI * 0.5f;
            Vector2 scale = Entity.Scale;
            Vector2 bodyPos = Entity.Transform.Position + LocalOffset + AdditionalOffset;
            Vector2 headPos = bodyPos + new Vector2(Mathf.Cos(r2), Mathf.Sin(r2)) * (-32f * scale.Y);
            const float DN = 0.01f;

            var effect = (scale.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally) | (scale.Y > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            if (scale.X < 0)
                scale.X = -scale.X;
            if (scale.Y < 0)
                scale.Y = -scale.Y;

            batcher.Draw(BodyCol, bodyPos, Color.Multiply(SkinColor), r, BodyCol.Origin, scale, effect, _layerDepth);
            batcher.Draw(BodyDetail, bodyPos, Color, r, BodyDetail.Origin, scale, effect, _layerDepth + DN * 1);
            batcher.Draw(BodyOut, bodyPos, Color, r, BodyOut.Origin, scale, effect, _layerDepth + DN * 2);
            
            batcher.Draw(HeadCol, headPos, Color.Multiply(SkinColor), r, HeadCol.Origin, scale, effect, _layerDepth + DN * 3);
            batcher.Draw(Face, headPos, Color, r, Face.Origin, scale, effect, _layerDepth + DN * 4);
            batcher.Draw(Hair, headPos, Color.Multiply(HairColor), r, Hair.Origin, scale, effect, _layerDepth + DN * 5);
            batcher.Draw(HeadOut, headPos, Color, r, HeadOut.Origin, scale, effect, _layerDepth + DN * 6);

            if (DrawHealthBar)
            {
                _health ??= Entity?.GetComponent<Health>();
                if (_health != null)
                {
                    Vector2 pos = headPos + new Vector2(0f, -55f * scale.Y);
                    HealthBarRenderer.Render(batcher, camera, pos, _health.CurrentHealth, _health.MaxHealth);
                }
            }
        }
    }
}
