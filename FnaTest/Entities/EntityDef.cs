using JDef;
using Microsoft.Xna.Framework;
using Nez;
using System.Collections.Generic;

namespace MTD.Entities
{
    /// <summary>
    /// Represents and entity to be spawned into the world.
    /// </summary>
    public class EntityDef : Def
    {
        public string Name;
        public Vector2 Scale;
        public Vector2 LocalPosition;
        public float LocalRotation;
        public uint UpdateInterval = 1;
        public int Tag;
        public int UpdateOrder;

        public List<ComponentDef> Components;

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

        public Entity Create(Scene scene, Entity parent = null)
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

            return created;
        }
    }
}
