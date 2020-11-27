// Copyright (C) The original author or authors
//
// This software may be modified and distributed under the terms
// of the zlib license.  See the LICENSE file for details.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Nez;
using Nez.Textures;
using SpriterDotNet;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SpriterDotNet.Providers;

namespace Spriter2Nez
{
    /// <summary>
    /// MonoGame Animator implementation. It has separate Draw and Update steps. 
    /// During the Update step all spatial infos are calculated (translated from Spriter values) and the Draw step only draws the calculated values.
    /// </summary>
    public class NezAnimator : Animator<Sprite, SoundEffect>, IDisposable
    {
        /// <summary>
        /// The default provider factory that is used if null
        /// is passed into the constructor (see <see cref="NezAnimator(SpriterEntity, IProviderFactory{Sprite, SoundEffect}, Stack{SpriteDrawInfo})"/>)
        /// </summary>
        public static DefaultProviderFactory<Sprite, SoundEffect> DefaultProviderFactory;

        /// <summary>
        /// Scale factor of the animator. Negative values flip the image.
        /// </summary>
        public virtual Vector2 Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                scaleAbs = new Vector2(Math.Abs(value.X), Math.Abs(value.Y));
            }
        }

        /// <summary>
        /// Rotation in radians.
        /// </summary>
        public virtual float Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                rotationSin = (float)Math.Sin(Rotation);
                rotationCos = (float)Math.Cos(Rotation);
            }
        }

        /// <summary>
        /// Position in pixels.
        /// </summary>
        public virtual Vector2 Position { get; set; }

        /// <summary>
        /// The drawing depth. Should be in the [0,1] interval.
        /// </summary>
        public virtual float Depth { get; set; } = DefaultDepth;

        /// <summary>
        /// The depth distance between different sprites of the same animation.
        /// </summary>
        public virtual float DeltaDepth { get; set; } = DefaultDeltaDepth;

        /// <summary>
        /// The color used to render all the sprites.
        /// </summary>
        public virtual Color Color { get; set; } = Color.White;

        /// <summary>
        /// Gets the current update index. Every time <see cref="Update(float)"/> is called,
        /// this is incremented by 1. It is used by the AnimatorRenderer to recalculate bounds.
        /// </summary>
        public uint UpdateCounter { get; private set; }

        protected Stack<SpriteDrawInfo> DrawInfoPool { get; set; }
        protected List<SpriteDrawInfo> DrawInfos { get; set; } = new List<SpriteDrawInfo>();

        private static readonly float DefaultDepth = 0.5f;
        private static readonly float DefaultDeltaDepth = -0.000001f;

        private float rotation;
        private float rotationSin;
        private float rotationCos;

        private Vector2 scale;
        private Vector2 scaleAbs;
        private readonly bool usingPoolStack;

        public NezAnimator
        (
            SpriterEntity entity,
            IProviderFactory<Sprite, SoundEffect> providerFactory = null,
            Stack<SpriteDrawInfo> drawInfoPool = null
        ) : base(entity, providerFactory ?? DefaultProviderFactory)
        {
            Scale = Vector2.One;
            Rotation = 0;

            usingPoolStack = drawInfoPool == null;
            DrawInfoPool = drawInfoPool ?? StackPool<SpriteDrawInfo>.Get();
        }

        /// <summary>
        /// Releases the draw stack back into the pool, and does other cleanup.
        /// Do not attempt to use the animator after calling this.
        /// </summary>
        public void Dispose()
        {
            // Way to check if already disposed.
            if (DrawInfoPool == null)
                return;

            if (usingPoolStack)
            {
                StackPool<SpriteDrawInfo>.Release(DrawInfoPool);
            }
            DrawInfoPool = null;
            DrawInfos.Clear();
            DrawInfos = null;
            Entity = null;
            CurrentAnimation = null;
            NextAnimation = null;
            FrameData = null;
            DataProvider = null;
            SoundProvider = null;
            SpriteProvider = null;
        }

        /// <summary>
        /// Draws the animation with the given SpriteBatch.
        /// </summary>
        public virtual void Draw(Batcher batcher, Camera camera)
        {
            for (int i = 0; i < DrawInfos.Count; ++i)
            {
                SpriteDrawInfo di = DrawInfos[i];
                DrawSprite(batcher, camera, di);
            }
        }

        protected virtual void DrawSprite(Batcher batcher, Camera camera, SpriteDrawInfo info)
        {
            Sprite spr = info.Drawable;
            if (spr == null)
                return;

            Vector2 scale = info.Scale;
            bool changeX = scale.X < 0f;
            bool changeY = scale.Y < 0f;
            bool fx = false;
            bool fy = false;
            var effect = SpriteEffects.None;
            if (changeX)
            {
                scale.X = -scale.X;
                fx = true;
            }
            if (changeY)
            {
                scale.Y = -scale.Y;
                fy = true;
            }
            if (changeX || changeY)
            {
                effect = fx ? (effect | SpriteEffects.FlipHorizontally) : (effect & ~SpriteEffects.FlipHorizontally);
                effect = fy ? (effect | SpriteEffects.FlipVertically) : (effect & ~SpriteEffects.FlipVertically);
            }

            Vector2 origin = info.Pivot * new Vector2(spr.SourceRect.Width, spr.SourceRect.Height);
            float d = info.Depth;
            if (d < 0f)
                d = 0f;
            batcher.Draw(spr, info.Position, info.Color, info.Rotation, origin, scale, effect, d);
        }

        public override void Update(float deltaTime)
        {
            for (int i = 0; i < DrawInfos.Count; ++i)
            {
                DrawInfoPool.Push(DrawInfos[i]);
            }
            DrawInfos.Clear();

            base.Update(deltaTime);

            UpdateCounter++;
        }

        protected override void ApplySpriteTransform(Sprite drawable, SpriterObject info)
        {
            GetPositionAndRotation(info, out float posX, out float posY, out float rot);

            SpriteDrawInfo di = DrawInfoPool.Count > 0 ? DrawInfoPool.Pop() : new SpriteDrawInfo();

            di.Drawable = drawable;
            di.Pivot = new Vector2(info.PivotX, (1 - info.PivotY));
            di.Position = new Vector2(posX, posY);
            di.Scale = new Vector2(info.ScaleX, info.ScaleY) * Scale;
            di.Rotation = rot;
            di.Color = Color * info.Alpha;
            di.Depth = Depth + DeltaDepth * DrawInfos.Count;

            DrawInfos.Add(di);
        }

        protected override void PlaySound(SoundEffect sound, SpriterSound info)
        {
            sound.Play(info.Volume, 0.0f, info.Panning);
        }

        public RectangleF GetBoundingBox(SpriterObject info, float width, float height)
        {
            float posX, posY, rotation;
            GetPositionAndRotation(info, out posX, out posY, out rotation);

            float w = width * info.ScaleX * Scale.X;
            float h = height * info.ScaleY * Scale.Y;

            float rs = Mathf.Sin(rotation);
            float rc = Mathf.Cos(rotation);

            Vector2 originDelta = Rotate(new Vector2(-info.PivotX * w, -(1 - info.PivotY) * h), rs, rc);

            Vector2 horizontal = Rotate(new Vector2(w, 0), rs, rc);
            var Point1 = new Vector2(posX, posY) + originDelta;
            var Point2 = Point1 + horizontal;
            var Point4 = Point1 + Rotate(new Vector2(0, h), rs, rc);
            var Point3 = Point4 + horizontal;

            float minX = Mathf.MinOf(Point1.X, Point2.X, Point3.X, Point4.X);
            float maxX = Mathf.MaxOf(Point1.X, Point2.X, Point3.X, Point4.X);
            float minY = Mathf.MinOf(Point1.Y, Point2.Y, Point3.Y, Point4.Y);
            float maxY = Mathf.MaxOf(Point1.Y, Point2.Y, Point3.Y, Point4.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        public Vector2 GetPosition(SpriterObject info)
        {
            float posX, posY, rotation;
            GetPositionAndRotation(info, out posX, out posY, out rotation);
            return new Vector2(posX, posY);
        }

        private void GetPositionAndRotation(SpriterObject info, out float posX, out float posY, out float rotation)
        {
            float px = info.X;
            float py = -info.Y;
            rotation = MathHelper.ToRadians(-info.Angle);

            if (Scale.X < 0)
            {
                px = -px;
                rotation = -rotation;
            }

            if (Scale.Y < 0)
            {
                py = -py;
                rotation = -rotation;
            }

            px *= scaleAbs.X;
            py *= scaleAbs.Y;

            rotation += Rotation;

            posX = px * rotationCos - py * rotationSin + Position.X;
            posY = px * rotationSin + py * rotationCos + Position.Y;
        }

        private static Vector2 Rotate(Vector2 v, float s, float c)
        {
            return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }
    }
}
