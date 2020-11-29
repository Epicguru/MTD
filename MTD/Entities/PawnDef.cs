using MTD.Components;
using Nez;

namespace MTD.Entities
{
    public abstract class PawnDef : EntityDef
    {
        public int Health = 100;
        public bool DoHurtEffect = true;

        public override Entity Create(Scene scene, Entity parent = null)
        {
            var e = base.Create(scene, parent);
            if (e == null)
                return null;

            var h = e.AddComponent(new Health(Health, Health));
            var p = e.AddComponent(CreatePawn());

            p.Health = h;
            h.UponHealthChangedEvent += (h, c, s) => p.UponHealthChanged(c, s);

            return e;
        }

        protected abstract Pawn CreatePawn();
    }
}
