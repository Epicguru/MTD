using Nez;

namespace FnaTest
{
    public static class NezExtensions
    {
        /// <summary>
        /// Returns true if this entity reference is null or if the entity is destroyed.
        /// </summary>
        public static bool IsNullOrDestroyed(this Entity entity)
        {
            return entity == null || entity.IsDestroyed;
        }

        /// <summary>
        /// Returns true if this component is null, is not part of an entity, or it's entity is destroyed.
        /// </summary>
        public static bool IsNullOrDestroyed(this Component component)
        {
            return component == null || (component.Entity?.IsDestroyed ?? true);
        }
    }
}
