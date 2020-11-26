using Microsoft.Xna.Framework;
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

        public float TrailLength;
        public float TrailThickness;
        public Color TrailColor;

        public Bullet(BulletDef def) : base(def)
        {

        }

        public override void Reset()
        {
            base.Reset();

            var bd = Def as BulletDef;
            TrailLength = bd.TrailLength;
            TrailThickness = bd.TrailThickness;
            TrailColor = bd.TrailColor;
        }

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

    public class BulletDef : ProjectileDef
    {
        public float TrailLength;
        public float TrailThickness;
        public Color TrailColor;

        protected override Projectile CreateProjectile()
        {
            return new Bullet(this);
        }
    }
}
