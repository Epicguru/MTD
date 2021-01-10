using Microsoft.Xna.Framework;
using Nez;
using Nez.Textures;

namespace MTD.Entities.Human
{
    public class HumanDef : SentientPawnDef
    {
        // Visuals
        public Sprite BodyCol, BodyDetail, BodyOut;
        public Sprite HeadCol, Face, Hair, HeadOut;
        public Color SkinColor, HairColor;

        // Collider
        public Vector2 ColliderSize;
        public Vector2 ColliderOffset;

        public override Entity Create(Scene scene, Entity parent = null)
        {
            var e = base.Create(scene, parent);
            if (e == null)
                return null;

            var human = e.GetComponent<Human>();
            var r = e.AddComponent(CreateRenderer());
            e.AddComponent(CreateCollider());
            human.Renderer = r;

            return e;
        }

        protected override Pawn CreatePawn()
        {
            return new Human(this);
        }

        private HumanRenderer CreateRenderer()
        {
            return new HumanRenderer()
            {
                BodyCol = BodyCol,
                BodyDetail = BodyDetail,
                BodyOut = BodyOut,
                HeadCol = HeadCol,
                Face = Face,
                Hair = Hair,
                HeadOut = HeadOut,
                SkinColor = SkinColor,
                HairColor = HairColor,
                LocalOffset = new Vector2(0, -32f)
            };
        }

        private BoxCollider CreateCollider()
        {
            var bc = new BoxCollider(ColliderSize.X, ColliderSize.Y);
            bc.LocalOffset += ColliderOffset;
            return bc;
        }
    }
}
