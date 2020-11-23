using Microsoft.Xna.Framework;
using MTD.World;
using Nez;

namespace MTD.Components
{
    public class Bullet : Projectile
    {
        public override RectangleF Bounds
        {
            get
            {
                return RectangleF.FromMinMax(TrailStart(), TrailEnd());
            }
        }

        public float TrailLength = Tile.SIZE * 1.5f;
        public float TrailThickness = 2;
        public Color TrailColor = Color.Yellow;

        private Vector2 TrailStart()
        {
            return Transform.Position;
        }

        private Vector2 TrailEnd()
        {
            return Transform.Position - base.Velocity.Normalized() * TrailLength;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            batcher.DrawLine(TrailStart(), TrailEnd(), TrailColor, TrailThickness);
        }

        public override void UponHit(RaycastHit hit)
        {
            base.UponHit(hit);

            Health h = hit.Collider?.Entity?.GetComponent<Health>();
            if (h == null)
                return;

            h.ChangeHealth(-10, "Bullet");
        }
    }
}
