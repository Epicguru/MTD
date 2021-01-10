using System.Collections.Generic;
using JDef;
using MTD.Components;
using MTD.Jobs;
using Nez;

namespace MTD.Entities
{
    public class SentientPawnDef : PawnDef
    {
        // Movement
        public float MovementSpeed;
        public bool FaceMovementDirection;

        // Goal providers.
        public List<Def> GoalProviders;

        public override Entity Create(Scene scene, Entity parent = null)
        {
            var e = base.Create(scene, parent);
            if (e == null)
                return null;

            var sp = e.GetComponent<SentientPawn>();

            // TODO add settings for speed, etc.
            var pf = e.AddComponent(CreatePathFollower());
            sp.PathFollower = pf;

            if (GoalProviders != null)
            {
                foreach (var raw in GoalProviders)
                {
                    var gp = raw as GoalProviderDef;
                    if (gp == null)
                        continue;

                    var created = gp.Create();
                    if (created == null)
                        continue;

                    sp.JobManager.AddProvider(created);
                }
            }

            return e;
        }

        private PathFollower CreatePathFollower()
        {
            return new PathFollower()
            {
                MovementSpeed = MovementSpeed,
                FaceMovementDirection = FaceMovementDirection
            };
        }

        protected override Pawn CreatePawn()
        {
            return new SentientPawn(this);
        }
    }
}
