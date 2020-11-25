using JDef;
using Microsoft.Xna.Framework;
using Nez;
using System.Collections.Generic;
using System.Linq;

namespace MTD.Entities
{
    /// <summary>
    /// Represents and entity to be spawned into the world.
    /// </summary>
    public class EntityDef : Def
    {
        private static readonly Dictionary<string, EntityDef> namedDefs = new Dictionary<string, EntityDef>();
        private static EntityDef[] allDefs;
        internal static void Load()
        {
            var db = Main.Defs;
            var all = db.GetAllOfType<EntityDef>();
            foreach (var def in all)
            {
                namedDefs.Add(def.DefName, def);
            }
            allDefs = namedDefs.Values.ToArray();
        }
        public static EntityDef Get(string defName)
        {
            if (namedDefs.TryGetValue(defName, out var def))
                return def;
            return null;
        }
        public static IReadOnlyList<EntityDef> GetAll()
        {
            return allDefs;
        }

        public string Name;
        public Vector2 Scale;
        public Vector2 LocalPosition;
        public float LocalRotation;
        public uint UpdateInterval = 1;
        public int Tag;
        public int UpdateOrder;

        public List<ComponentDef> Components;
        public List<Def> Children;

        public override void Validate()
        {
            base.Validate();

            if (Name == null)
            {
                ValidateWarn("Null entity Name. Using 'Default Name' instead.");
                Name = "Default Name";
            }

            if (Components != null)
            {
                foreach (var comp in Components)
                {
                    comp?.ValidateInt(this);
                }
            }
        }

        public virtual Entity Create(Scene scene, Entity parent = null)
        {
            if (scene == null)
            {
                Debug.Error("Scene null, cannot create entity from this entity def.");
                return null;
            }

            var created = scene.CreateEntity(this.Name);

            created.Tag = Tag;
            created.UpdateOrder = UpdateOrder;
            created.UpdateInterval = UpdateInterval;

            if (Components != null)
            {
                foreach (var comp in Components)
                {
                    if (comp == null)
                        continue;

                    var createdComp = comp.CreateInt();
                    created.AddComponent(createdComp);
                }
            }

            if (!parent.IsNullOrDestroyed())
            {
                created.SetParent(parent);
                created.LocalPosition = LocalPosition;
                created.LocalRotationDegrees = LocalRotation;
            }

            if (Children != null)
            {
                foreach (var child in Children)
                {
                    if(child is EntityDef def)
                        def.Create(scene, created);
                }
            }

            return created;
        }
    }
}
