using Microsoft.Xna.Framework;
using Nez.Sprites;
using Nez.Textures;
using System;
using Nez;
using MTD.Components;

namespace MTD.Entities
{
    public abstract class ComponentDef
    {
        public abstract Type Class { get; }

        public override bool Equals(object obj)
        {
            if (obj is ComponentDef otherDef)
                return this.Class == otherDef.Class;
            return false;
        }

        internal Component CreateInt()
        {
            var value = Create();
            if (value == null)
                return null;

            ApplyValues(value);
            return value;
        }

        private EntityDef tempEDef;

        internal void ValidateInt(EntityDef eDef)
        {
            tempEDef = eDef;
            Validate();
            tempEDef = null;
        }

        public virtual void Validate()
        {
            
        }

        protected void ValidateWarn(string msg, Exception e = null)
        {
            tempEDef?.ValidateWarn($"[{Class?.Name}] {msg}");
        }

        protected void ValidateError(string msg, Exception e = null)
        {
            tempEDef?.ValidateError($"[{Class?.Name}] {msg}", e);
        }

        public abstract Component Create();

        public virtual void ApplyValues(Component c)
        {

        }

        public override int GetHashCode()
        {
            return Class.GetHashCode();
        }
    }

    public abstract class RenderableComponentDef : ComponentDef
    {
        public Color Color = new Color(255, 255, 255, 255);
        public Vector2 LocalPosition = Vector2.Zero;
        public float LayerDepth;
        public int RenderLayer;

        public override void ApplyValues(Component c)
        {
            if (!(c is RenderableComponent comp))
                return;

            comp.Color = Color;
            comp.LocalOffset = LocalPosition;
            comp.RenderLayer = RenderLayer;
            comp.LayerDepth = LayerDepth;
        }

        public override void Validate()
        {
            base.Validate();

            if (LayerDepth < 0 || LayerDepth > 1)
            {
                ValidateWarn($"LayerDepth {LayerDepth} is outside of the valid depth, 0 to 1 inclusive. Value will be clamped.");
                LayerDepth = MathHelper.Clamp(LayerDepth, 0f, 1f);
            }
        }
    }

    public class SpriteRendererDef : RenderableComponentDef
    {
        public override Type Class => typeof(SpriteRenderer);

        public Sprite Sprite;
        public bool FlipX, FlipY;

        public override Component Create()
        {
            var comp = new SpriteRenderer(Sprite)
            {
                FlipX = FlipX,
                FlipY = FlipY
            };
            return comp;
        }
    }

    public class HealthDef : ComponentDef
    {
        public override Type Class => typeof(Health);

        public int MaxHealth = 100;
        public int StartHealth = 100;
        public bool StartWithMaxHealth = true;
        public bool DestroyUponDeath = true;

        public override Component Create()
        {
            return new Health(StartWithMaxHealth ? MaxHealth : StartHealth, MaxHealth)
            {
                DestroyUponDeath = DestroyUponDeath
            };
        }
    }
}
