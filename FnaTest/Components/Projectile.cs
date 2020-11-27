using Microsoft.Xna.Framework;
using MTD.Entities;
using Nez;
using System.Collections.Generic;
using System.Linq;
using MTD.World;

namespace MTD.Components
{
    public abstract class Projectile : RenderableComponent, IUpdatable
    {
        #region Static Methods

        public static IEnumerable<(ProjectileDef type, int count)> PoolCounts()
        {
            foreach (var pair in pools)
            {
                yield return (pair.Key, pair.Value.Count);
            }
        }
        private static readonly Dictionary<ProjectileDef, Queue<Projectile>> pools = new Dictionary<ProjectileDef, Queue<Projectile>>();

        public static Projectile Spawn(ProjectileDef def, Vector2 position, float angle)
        {
            return Spawn<Projectile>(def, position, angle);
        }

        public static Projectile Spawn(ProjectileDef def, Vector2 position, Vector2 direction)
        {
            return Spawn<Projectile>(def, position, direction);
        }

        public static T Spawn<T>(ProjectileDef def, Vector2 position, float angle) where T : Projectile
        {
            return Spawn<T>(def, position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }

        public static T Spawn<T>(ProjectileDef def, Vector2 position, Vector2 direction) where T : Projectile
        {
            if (def == null)
                return null;

            var created = GetOrCreate<T>(Core.Scene, def);
            if (created == null)
                return null;

            created.Entity.Position = position;
            created.Velocity = direction.Normalized() * created.BaseSpeed;

            return created;
        }

        public static void Despawn(Projectile p)
        {
            var def = p?.Def;
            if (def == null)
                return;

            if (p.Entity.Scene == null)
            {
                Debug.Error("Projectile already despawned!");
                return;
            }

            // Detatch from scene. Nez API is actually sometimes good!
            p.Entity.DetachFromScene();

            if (pools.TryGetValue(def, out var pool))
            {
                pool.Enqueue(p);
            }
            else
            {
                pool = new Queue<Projectile>();
                pool.Enqueue(p);
                pools.Add(def, pool);
            }
        }

        private static T GetOrCreate<T>(Scene scene, ProjectileDef def) where T : Projectile
        {
            if (def == null)
                return null;

            Projectile toReturn;
            if (pools.TryGetValue(def, out var pool))
            {
                if (pool.Count > 0)
                {
                    toReturn = pool.Dequeue();
                    toReturn.Reset();
                }
                else
                {
                    toReturn = def.Create(scene)?.GetComponent<Projectile>(); // Freshly created projectile are already reset.
                }
            }
            else
            {
                toReturn = def.Create(scene)?.GetComponent<Projectile>();
            }

            if (toReturn == null)
                return null;

            if (toReturn.Entity.Scene == null)
                toReturn.Entity.AttachToScene(scene);
            
            return toReturn as T;
        }

        #endregion

        public readonly ProjectileDef Def;
        public float BaseSpeed
        {
            get
            {
                return Def.BaseSpeed;
            }
        }

        public float Lifetime;
        public Vector2 Velocity;
        public Vector2 Gravity;
        public float Speed
        {
            get
            {
                return Velocity.Length();
            }
            set
            {
                if (value < 0)
                    value = 0f;
                Velocity = Velocity.Normalized() * value;
            }
        }

        private float timer;

        protected Projectile(ProjectileDef def)
        {
            this.Def = def;
        }

        public virtual void Reset()
        {
            Velocity = new Vector2(BaseSpeed, 0); // Just to have some movement.
            Gravity = Def.Gravity;
            Lifetime = Def.Lifetime;
            timer = 0f;
        }

        public virtual void Update()
        {
            // Check timer.
            timer += Time.DeltaTime;
            if (timer > Lifetime)
            {
                Despawn();
                return;
            }

            // Add gravity.
            Velocity += Time.DeltaTime * Gravity;

            var pos = Transform.Position;
            var nextPos = pos + (Velocity * Tile.SIZE) * Time.DeltaTime;

            var hit = Physics.Linecast(pos, nextPos);
            if (hit.Collider != null)
            {
                UponHit(hit);
                return;
            }
            Transform.Position = nextPos;
        }

        public virtual void UponHit(RaycastHit hit)
        {
            Despawn();
        }

        public void Despawn()
        {
            Despawn(this);
        }
    }

    public abstract class ProjectileDef : EntityDef
    {
        #region Static Methods 

        public new static IReadOnlyList<ProjectileDef> All { get { return allDefs; } }
        private static readonly Dictionary<string, ProjectileDef> namedDefs = new Dictionary<string, ProjectileDef>();
        private static ProjectileDef[] allDefs;

        internal new static void Load()
        {
            var db = Main.Defs;
            var all = db.GetAllOfType<ProjectileDef>();
            foreach (var def in all)
            {
                namedDefs.Add(def.DefName, def);
            }
            allDefs = namedDefs.Values.ToArray();
        }

        public new static ProjectileDef Get(string defName)
        {
            if (namedDefs.TryGetValue(defName, out var def))
                return def;
            return null;
        }

        #endregion

        public float Lifetime;
        public Vector2 Gravity;
        public float BaseSpeed;

        public override Entity Create(Scene scene, Entity parent = null)
        {
            var e = base.Create(scene, parent);
            if (e == null)
                return null;

            var p = e.AddComponent(CreateProjectile());
            p.Reset();

            return e;
        }

        protected abstract Projectile CreateProjectile();
    }
}
