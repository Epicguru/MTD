using Microsoft.Xna.Framework;
using Nez;

namespace MTD.Components
{
    public class Projectile : Component, IUpdatable
    {
        public Vector2 Velocity;

        public virtual void Update()
        {
            var pos = Transform.Position;

            var nextPos = pos + Velocity * Time.DeltaTime;

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
            Entity.Destroy();
        }
    }
}
