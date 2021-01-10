using JDef;
using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using MTD.World;

namespace MTD.Effects
{
    /// <summary>
    /// Motes are little sprites or animations that can be spawned into the world
    /// to show the user that something is happening, such as a tile being mined by a pawn
    /// or an interaction happening.
    /// </summary>
    public abstract class Mote
    {
        #region Static Methods

        public static int ActiveMoteCount
        {
            get { return activeMotes.Count; }
        }
        public static IEnumerable<(MoteDef type, int count)> PoolCounts()
        {
            foreach (var pair in pools)
            {
                yield return (pair.Key, pair.Value.Count);
            }
        }
        private static readonly Dictionary<MoteDef, Queue<Mote>> pools = new Dictionary<MoteDef, Queue<Mote>>();
        private static readonly List<Mote> activeMotes = new List<Mote>();

        public static Mote Spawn(MoteDef def, Vector2 position)
        {
            return Spawn<Mote>(def, position);
        }

        public static T Spawn<T>(MoteDef def, Vector2 position) where T : Mote
        {
            if (def == null)
                return null;

            var created = GetOrCreate<T>(def);
            if (created == null)
                return null;

            activeMotes.Add(created);
            created.IsActive = true;

            created.Position = position;
            created.Maintain();

            return created;
        }

        private static void Despawn(Mote m)
        {
            var def = m?.Def;
            if (def == null)
                return;

            if (pools.TryGetValue(def, out var pool))
            {
                pool.Enqueue(m);
            }
            else
            {
                pool = new Queue<Mote>();
                pool.Enqueue(m);
                pools.Add(def, pool);
            }

            m.IsActive = false;
        }

        private static T GetOrCreate<T>(MoteDef def) where T : Mote
        {
            if (def == null)
                return null;

            Mote toReturn;
            if (pools.TryGetValue(def, out var pool))
            {
                if (pool.Count > 0)
                {
                    toReturn = pool.Dequeue();
                }
                else
                {
                    toReturn = def.Create();
                }
            }
            else
            {
                toReturn = def.Create();
            }

            if (toReturn == null)
                return null;

            toReturn.Reset();

            return toReturn as T;
        }

        public static void ClearAll()
        {
            activeMotes.Clear();
            pools.Clear();
        }

        public static void DrawAll(Batcher batcher, Camera camera)
        {
            for(int i = 0; i < activeMotes.Count; i++)
            {
                var mote = activeMotes[i];

                float depth = (float) i / (activeMotes.Count);
                bool active = mote.Draw(batcher, camera, depth);

                mote.TimeSinceMaintain += Time.DeltaTime;

                if (!active)
                {
                    Despawn(mote);
                    activeMotes.RemoveAt(i);
                    i--;
                }
            }
        }

        #endregion

        public readonly MoteDef Def;
        public Vector2 Position;
        public float Rotation;
        public Vector2 Scale;

        public bool IsActive { get; private set; }
        public float TimeSinceMaintain { get; protected set; }

        public virtual float FadePercentage
        {
            get
            {
                if (TimeSinceMaintain <= Def.FadeStartTime)
                    return 0f;
                if (TimeSinceMaintain <= Def.FadeEndTime)
                {
                    if (Def.FadeEndTime <= 0f)
                        return 1f;

                    return Mathf.Clamp01((TimeSinceMaintain - Def.FadeStartTime) / (Def.FadeEndTime - Def.FadeStartTime));
                }
                // Greater than fade end time.
                return 1f;
            }
        }

        protected Mote(MoteDef def)
        {
            this.Def = def;
        }

        public virtual void Reset()
        {
            TimeSinceMaintain = 0f;
            Position = Vector2.Zero;
            Rotation = 0f;
            Scale = Vector2.One;
        }

        public virtual void Maintain()
        {
            if (!IsActive)
                throw new Exception("Cannot call Maintain() on this mote, it is no longer active!");

            TimeSinceMaintain = 0f;
        }

        /// <summary>
        /// Called every frame when the mote is active.
        /// Should draw the mote at the current position, rotation and scale, using the supplied depth.
        /// Well-behaved motes will also cull themselves using the camera.
        ///
        /// Return true to keep the mote alive, or false to despawn the mote.
        /// </summary>
        /// <param name="batcher">The batcher to draw with.</param>
        /// <param name="camera">The camera. Use to cull.</param>
        /// <param name="depth">The depth to render at.</param>
        /// <returns>True to keep mote alive and active, false to return the mote to the pool.</returns>
        public abstract bool Draw(Batcher batcher, Camera camera, float depth);
    }

    public abstract class MoteDef : Def
    {
        #region Static Methods

        public static IReadOnlyList<MoteDef> AllDefs { get { return allDefs; } }
        private static MoteDef[] allDefs;
        private static Dictionary<string, MoteDef> namedDefs;

        public static MoteDef Get(string name)
        {
            if (name == null)
                return null;
            if (namedDefs.TryGetValue(name, out var found))
                return found;
            return null;
        }

        public static void Load()
        {
            if (allDefs != null)
                return;

            var all = Main.Defs.GetAllOfType<MoteDef>().ToArray();
            allDefs = all;
            namedDefs = new Dictionary<string, MoteDef>();
            foreach (var def in all)
            {
                namedDefs.Add(def.DefName, def);
            }
        }

        #endregion

        public Type Class;
        public float FadeStartTime;
        public float FadeEndTime;

        public override void Validate()
        {
            base.Validate();

            if (Class == null)
            {
                ValidateError("Class is null.");
            }
            else if (!typeof(Mote).IsAssignableFrom(Class))
            {
                ValidateError($"{Class.FullName} does not inherit from Mote.");
            }

            if (FadeEndTime < 0f)
            {
                ValidateWarn("FadeEndTime must not be negative.");
                FadeEndTime = 0f;
            }
            if (FadeStartTime > FadeEndTime)
            {
                ValidateWarn("FadeStartTime must be less than the fade end time.");
                FadeStartTime = FadeEndTime;
            }
        }

        public abstract Mote Create();
    }

    internal class MoteRenderer : RenderableComponent
    {
        // Do not cull.
        public override RectangleF Bounds => RectangleF.MaxRect;

        public MoteRenderer()
        {
            base.LayerDepth = 0f;
            base.RenderLayer = Main.LAYER_MOTES;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            Mote.DrawAll(batcher, camera);

            // TODO put this somewhere better!
            Map.Current.DrawOverlays(batcher, camera);
        }
    }
}
