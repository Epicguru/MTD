﻿using Microsoft.Xna.Framework;
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

        public int UpdateOrder;

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
            c.UpdateOrder = UpdateOrder;
        }

        public override int GetHashCode()
        {
            return Class.GetHashCode();
        }
    }

    public class SubSpriteDef : EntityDef
    {
        public Sprite Sprite;
        public bool FlipX, FlipY;
        public Color Color;
        public Vector2 OriginNormalized = new Vector2(-1, -1);

        public override Entity Create(Scene scene, Entity parent = null)
        {
            var e = base.Create(scene, parent);
            if (e == null)
                return null;

            var spr = e.GetComponent<SpriteRenderer>();
            if (spr != null)
            {
                spr.Sprite = Sprite;
                spr.Color = Color;
                if (OriginNormalized.X != -1 && Sprite != null)
                {
                    float x = OriginNormalized.X;
                    float y = OriginNormalized.Y;
                    spr.Origin = new Vector2(x * Sprite.SourceRect.Width, y * Sprite.SourceRect.Height);
                }
                spr.FlipX = FlipX;
                spr.FlipY = FlipY;
            }

            return e;
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
        public Vector2 OriginNormalized = new Vector2(-1, -1);

        public override Component Create()
        {
            var comp = new SpriteRenderer(Sprite)
            {
                FlipX = FlipX,
                FlipY = FlipY

            };
            if (OriginNormalized.X != -1 && Sprite != null)
            {
                float x = OriginNormalized.X;
                float y = OriginNormalized.Y;
                comp.Origin = new Vector2(x * Sprite.SourceRect.Width, y * Sprite.SourceRect.Height);
            }
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
        public bool AllowHealAfterDeath = false;

        public override Component Create()
        {
            return new Health(StartWithMaxHealth ? MaxHealth : StartHealth, MaxHealth)
            {
                DestroyUponDeath = DestroyUponDeath,
                AllowHealAfterDeath = AllowHealAfterDeath
            };
        }
    }

    public class PathFollowerDef : ComponentDef
    {
        public override Type Class => typeof(PathFollower);

        public float MovementSpeed = 5f;
        public bool FaceMovementDirection = true;

        public override Component Create()
        {
            return new PathFollower()
            {
                MovementSpeed = this.MovementSpeed,
                FaceMovementDirection = this.FaceMovementDirection
            };
        }

        public override void Validate()
        {
            base.Validate();

            if (MovementSpeed <= 0)
            {
                ValidateError($"Path follower MovementSpeed must be greater than zero! Value: {MovementSpeed}");
            }
        }
    }

    public abstract class ColliderDef : ComponentDef
    {
        public Vector2 LocalOffset = new Vector2();
        public bool ScaleAndRotateWithTransform = true;
        public bool IsTrigger = false;

        public override void ApplyValues(Component c)
        {
            base.ApplyValues(c);

            if (c is Collider coll)
            {
                coll.LocalOffset = LocalOffset;
                coll.SetShouldColliderScaleAndRotateWithTransform(ScaleAndRotateWithTransform);
                coll.IsTrigger = IsTrigger;
            }
        }
    }

    public class BoxColliderDef : ColliderDef
    {
        public override Type Class => typeof(BoxCollider);

        public Vector2 Size = new Vector2(32, 32);

        public override Component Create()
        {
            return new BoxCollider(Size.X, Size.Y);
        }
    }
}
