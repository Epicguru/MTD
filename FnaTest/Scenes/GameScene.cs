using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MTD.World;
using Nez;
using Nez.ImGuiTools;

namespace MTD.Scenes
{
    public class GameScene : Scene
    {
        public static TileLayer GenerateLayer(int w, int h)
        {
            var layer = new TileLayer(w, h);

            var dirt = Main.Defs.GetNamed<TileDef>("DirtTile");
            var stone = Main.Defs.GetNamed<TileDef>("StoneTile");

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (Random.Chance(0.5f))
                    {
                        layer.SetTile(x, y, dirt);
                    }
                    else if (Random.Chance(0.5f))
                    {
                        layer.SetTile(x, y, stone);
                    }
                }
            }

            return layer;
        }

        public TileLayer Layer;

        public override void Initialize()
        {
            base.Initialize();

            AddRenderer(new DefaultRenderer());
        }

        public override void OnStart()
        {
            base.OnStart();

            var mapEnt = CreateEntity("Map");
            var layerEnt = CreateEntity("Layer 0");

            layerEnt.Parent = mapEnt.Transform;
            layerEnt.AddComponent(Layer);
        }

        public override void Update()
        {
            Camera.MinimumZoom = 0.01f;

            Vector2 vel = new Vector2();
            if (Input.IsKeyDown(Keys.A))
                vel.X -= 1;
            if (Input.IsKeyDown(Keys.D))
                vel.X += 1;
            if (Input.IsKeyDown(Keys.W))
                vel.Y -= 1;
            if (Input.IsKeyDown(Keys.S))
                vel.Y += 1;

            if (Input.IsKeyDown(Keys.E))
                Camera.Zoom += Time.UnscaledDeltaTime * 0.5f;
            if (Input.IsKeyDown(Keys.Q))
                Camera.Zoom -= Time.UnscaledDeltaTime * 0.5f;

            vel.Normalize();
            vel *= Time.UnscaledDeltaTime * Tile.SIZE * 10f;

            Camera.Position += vel;

            base.Update();
        }
    }
}
