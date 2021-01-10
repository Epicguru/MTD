using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MTD.Jobs.Goals;
using MTD.World;
using Nez;
using Nez.ImGuiTools.ObjectInspectors;

namespace MTD.Entities.Human
{
    public class Human : SentientPawn
    {
        public HumanRenderer Renderer { get; internal set; }
        public HumanDef HumanDef
        {
            get
            {
                return base.Def as HumanDef;
            }
        }

        public Human(HumanDef def) : base(def)
        {

        }

        public override void Update()
        {
            base.Update();

            Renderer.Color = Color.Lerp(Color.White, Color.Red, base.hurtEffectLerp);
            if (hurtEffectLerp > 0)
                Renderer.AdditionalOffset = Random.PointOnCircle() * hurtEffectLerp * 5f;
            else
                Renderer.AdditionalOffset = Vector2.Zero;

            Renderer.DrawHealthBar = TimeSinceHit < 8f;

            if (Input.IsKeyPressed(Keys.O))
            {
                var target = Map.Current.WorldToTileCoordinates(Input.WorldMousePos);
                JobManager.InterruptCurrent(new MoveToGoal(target));
            }

            if (Input.IsKeyPressed(Keys.I))
            {
                var target = Map.Current.WorldToTileCoordinates(Input.WorldMousePos);
                JobManager.InterruptCurrent(new RemoveTileGoal(target.X, target.Y, 0));
            }
        }

        [InspectorDelegate]
        private void DrawInspector()
        {
            ImGui.TextWrapped(JobManager.CurrentGoal?.DebugString ?? "No goal.");
        }
    }
}
