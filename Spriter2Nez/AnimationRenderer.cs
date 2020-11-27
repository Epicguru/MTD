using Nez;
using Nez.Textures;
using SpriterDotNet;

namespace Spriter2Nez
{
    public class AnimationRenderer : RenderableComponent, IUpdatable
    {
        public override RectangleF Bounds
        {
            get
            {
                if (Animator == null)
                    return RectangleF.Empty; // Nothing to draw, this should cause the drawer to be culled.

                if (Animator.FrameData == null)
                    return _cachedBounds;

                if (Animator.UpdateCounter != lastBoundsCalcFrame)
                {
                    lastBoundsCalcFrame = Animator.UpdateCounter;

                    bool first = true;
                    foreach (var thing in Animator.FrameData.SpriteData)
                    {
                        Sprite sprite = Animator.SpriteProvider.Get(thing.FolderId, thing.FileId);
                        var bounds = Animator.GetBoundingBox(thing, sprite.SourceRect.Width, sprite.SourceRect.Height);
                        if (first)
                        {
                            first = false;
                            _cachedBounds = bounds;
                        }
                        else
                        {
                            _cachedBounds = RectangleF.Union(_cachedBounds, bounds);
                        }
                    }
                }

                return _cachedBounds;
            }
        }

        /// <summary>
        /// Helper property to get the current animation. May return null.
        /// Shorthand for:
        /// <code>Animator?.CurrentAnimation</code>
        /// </summary>
        public SpriterAnimation CurrentAnimation
        {
            get
            {
                return Animator?.CurrentAnimation;
            }
        }

        /// <summary>
        /// Helper property to get the current animation's name. May return null.
        /// Shorthand for:
        /// <code>Animator?.CurrentAnimation?.Name</code>
        /// </summary>
        public string CurrentAnimationName
        {
            get
            {
                return CurrentAnimation?.Name;
            }
        }

        /// <summary>
        /// Helper property to get the next animation.
        /// Will return null if the animator is not transitioning or blending.
        /// Shorthand for:
        /// <code>Animator?.NextAnimation</code>
        /// </summary>
        public SpriterAnimation NextAnimation
        {
            get
            {
                return Animator?.NextAnimation;
            }
        }

        /// <summary>
        /// Helper property to get the next animation's name.
        /// Will return null if the animator is not transitioning or blending.
        /// Shorthand for:
        /// <code>Animator?.NextAnimation?.Name</code>
        /// </summary>
        public string NextAnimationName
        {
            get
            {
                return NextAnimation?.Name;
            }
        }

        /// <summary>
        /// Gets or sets the current animator speed.
        /// Value of 0 means that the animator is paused, value of 1 means normal speed, value of 0.5 means
        /// half speed.
        /// Also supports negative values to play animations backwards.
        /// Note that the actual speed of playback may be affected by <see cref="Time.TimeScale"/> depending
        /// on the <see cref="UseUnscaledDeltaTime"/> property.
        /// </summary>
        public float Speed
        {
            get
            {
                return Animator?.Speed ?? 1f;
            }
            set
            {
                if (Animator != null)
                    Animator.Speed = value;
            }
        }

        /// <summary>
        /// The currently active <see cref="NezAnimator"/>.
        /// Might be null.
        /// </summary>
        public NezAnimator Animator;
        /// <summary>
        /// If true, this Animator is not affected by <see cref="Time.TimeScale"/>.
        /// Is false by default.
        /// </summary>
        public bool UseUnscaledDeltaTime = false;
        /// <summary>
        /// If true, when this renderer is removed from it's entity (such as upon entity destruction),
        /// then the current <see cref="Animator"/> is disposed.
        /// Note that if the animator is swapped out after creation, then those that were swapped out will
        /// have to be manually disposed. 
        /// </summary>
        public bool DisposeAnimator = true;

        private RectangleF _cachedBounds;
        private uint lastBoundsCalcFrame = 69420; // Just not zero.

        public AnimationRenderer()
        {
            // Set to half because otherwise sprites can get clipped due to their depth being < 0.
            base.LayerDepth = 0.5f;
        }

        public AnimationRenderer(NezAnimator animator) : this()
        {
            this.Animator = animator;
        }

        public AnimationRenderer(SpriterEntity entity) : this()
        {
            if (entity != null)
                this.Animator = new NezAnimator(entity);
            else
                Debug.Warn("Null SpriterEntity passed into AnimationRenderer!");
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (DisposeAnimator)
            {
                Animator?.Dispose();
                Animator = null;
            }
        }

        public void Update()
        {
            if (Animator == null)
                return;

            Animator.Scale = Entity.Scale;
            Animator.Rotation = Entity.Rotation;
            Animator.Position = Entity.Position + base.LocalOffset;
            Animator.Color = base.Color;
            Animator.Depth = base.LayerDepth;

            float dt = UseUnscaledDeltaTime ? Time.UnscaledDeltaTime : Time.DeltaTime;
            Animator.Update(dt * 1000f); // Turn seconds into milliseconds.
        }

        /// <summary>
        /// Pauses the animator by setting the <see cref="Animator.Speed"/> to zero.
        /// </summary>
        public void Pause()
        {
            if (Animator == null)
                return;

            Animator.Speed = 0f;
        }

        /// <summary>
        /// Resumes animator playback by setting speed to 1.
        /// Only has any effect if the speed was zero (such as after calling <see cref="Pause"/>).
        /// </summary>
        public void Resume()
        {
            if (Animator == null)
                return;

            if (Animator.Speed == 0f)
                Animator.Speed = 1f;
        }

        /// <summary>
        /// Instantly starts playback of a named animation, with no transition time.
        /// Will reset playback to start of animation, even if the animation was already playing.
        /// See <see cref="CurrentAnimationName"/>.
        /// </summary>
        /// <param name="animName">The name of the Spriter animation.</param>
        public void Play(string animName)
        {
            if (Animator == null)
                return;
            if (string.IsNullOrEmpty(animName))
            {
                Debug.Warn("Null or blank animName parameter");
                return;
            }

            Animator.Play(animName);
        }

        /// <summary>
        /// Starts transitioning from the current animation to the target one.
        /// <para></para>
        /// Once the transition is complete, the <see cref="NextAnimation"/> will become
        /// the <see cref="CurrentAnimation"/>.
        /// The actual duration of the transition will depend on <see cref="Speed"/> and possibly
        /// <see cref="Time.TimeScale"/>.
        /// <para>The transition time is measured in MILLISECONDS!</para>
        /// </summary>
        /// <param name="targetName">The name of the target animation</param>
        /// <param name="time">The time, in MILLISECONDS, that the transition should have,</param>
        public void Transition(string targetName, float time)
        {
            if (Animator == null)
                return;
            if (string.IsNullOrEmpty(targetName))
            {
                Debug.Warn("Null or blank animName parameter");
                return;
            }

            // If the blend time is zero, just play().
            if (time <= 0f)
            {
                Play(targetName);
                return;
            }

            Animator.Transition(targetName, time);
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            Animator?.Draw(batcher, camera);
        }
    }
}
