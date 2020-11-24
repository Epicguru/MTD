using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MTD.Components;
using MTD.Entities;
using MTD.World;
using MTD.World.Pathfinding;
using Nez;
using Nez.ImGuiTools;
using System;
using MTD.World.Light;
using Nez.Sprites;
using Nez.Textures;
using Random = Nez.Random;

namespace MTD.Scenes
{
    public class GameScene : Scene
    {
        public static void GenerateMap(Map map)
        {
            int w = map.WidthInTiles;
            int h = map.HeightInTiles;

            var dirt = Main.Defs.GetNamed<TileDef>("DirtTile");
            var stone = Main.Defs.GetNamed<TileDef>("StoneTile");

            int height = h / 2;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (y >= h - height / 2)
                    {
                        map.SetTile(x, y, 0, stone, true);
                        map.SetTile(x, y, 1, stone, true);
                    }
                    else if (y >= h - height)
                    {
                        map.SetTile(x, y, 0, dirt, true);
                        if(y > h - height)
                            map.SetTile(x, y, 1, dirt, true);
                    }

                }
                if (Random.Chance(0.3f))
                    height += Random.Range(-1, 2);
            }
        }

        public static Effect TilesShader;

        public Map Map { get; internal set; }
        public UICanvas Canvas { get; private set; }
        public SkyLight SkyLight
        {
            get
            {
                return skyLightComp?.SL;
            }
        }
        public LightPP LightPostProcessor { get; private set; }
        public RenderLayerRenderer LightRenderer { get; private set; }
        public LightManager LightManager { get; private set; }

        private SkyLightComp skyLightComp;
        private Material tilesMat;
        private Light light;

        public override void Initialize()
        {
            base.Initialize();

            var otherMat = new Material() {SamplerState = SamplerState.LinearClamp};
            tilesMat = new Material() {SamplerState = SamplerState.LinearClamp, Effect=TilesShader};

            base.ClearColor = Color.CornflowerBlue;
            base.ClearColor.A = 0;

            AddRenderer(new RenderLayerExcludeRenderer(0, Main.LAYER_UI, Main.LAYER_TILES, Main.LAYER_LIGHT){Material=otherMat}); // For world objects.
            AddRenderer(new RenderLayerRenderer(-100, Main.LAYER_TILES){Material=tilesMat}); // Only renders map, using custom shader.
            AddRenderer(LightRenderer = new RenderLayerRenderer(99, Main.LAYER_LIGHT){RenderTargetClearColor = new Color(0, 0, 0, 255)}); // Light layer.
            AddRenderer(new ScreenSpaceRenderer(100, Main.LAYER_UI)); // For UI.
        }

        public override void OnStart()
        {
            base.OnStart();

            // Create stuff for ticking tiles and entities.

            if (Map == null)
                throw new Exception("Map is null! Should have been created before scene start.");

            if (Main.Pathfinder != null)
                throw new Exception("Expected Main.Pathfinder to be null, but it was not.");

            Main.Pathfinder = new Pathfinder(Main.PathfindingThreadCount);
            Main.Pathfinder.Start(Map);

            var mapEnt = CreateEntity("Map");
            Map.RenderLayer = Main.LAYER_TILES;
            mapEnt.AddComponent(Map);

            Core.GetGlobalManager<ImGuiManager>().RegisterDrawCommand(DrawImGui);

            TilesShader = Content.LoadEffect("Shaders/TileShader.fxb");
            tilesMat.Effect = TilesShader;

            LightPostProcessor = AddPostProcessor(new LightPP(0));
            LightRenderer.RenderTexture = LightPostProcessor.GetRenderTexture();

            skyLightComp = CreateEntity("SkyLight").AddComponent(new SkyLightComp(Map.WidthInTiles));
            SkyLight.Start(); // Starts running thread.

            LightManager = CreateEntity("Light manager").AddComponent(new LightManager());

            Tile.LoadMasks(Main.Atlas);

            CreateUI(CreateEntity("UI"));

            Camera.Position = new Vector2(Map.Width, Map.Height) * 0.5f;
        }

        public void CreateUI(Entity target)
        {
            Canvas = GameSceneUI.Setup(this, Main.UIAtlas);
            target.AddComponent(Canvas);
            UIScaleController.RegisterCanvas(Canvas);
        }

        public override void Unload()
        {
            // Stop running skylight before anything else, as this depends on map.
            skyLightComp.Dispose();
            skyLightComp = null;

            LightManager.Dispose();
            LightManager = null;

            var path = Main.Pathfinder;
            Main.Pathfinder = null;
            path.Dispose();

            UIScaleController.RemoveCanvas(Canvas);
            Canvas = null;

            TilesShader = null; // Will be automatically disposed.
            LightRenderer = null;
            LightPostProcessor = null;
            LightRenderer = null;

            // TODO unload/dispose world.

            base.Unload();
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

            if (Input.IsKeyDown(Keys.Space))
            {
                var pos = Map.MouseTileCoordinates;
                Map.SetTile(pos.X, pos.Y, 0, null);
            }
            if (Input.IsKeyDown(Keys.V))
            {
                var pos = Map.MouseTileCoordinates;
                Map.SetTile(pos.X, pos.Y, 0, TileDef.Get("LadderTile"));
            }
            if (Input.IsKeyDown(Keys.B))
            {
                var pos = Map.MouseTileCoordinates;
                Map.SetTile(pos.X, pos.Y, 0, TileDef.Get("StoneTile"));
            }
            if (Input.IsKeyDown(Keys.LeftAlt))
            {
                var pos = Map.MouseTileCoordinates;
                Map.AutoSlope(pos.X, pos.Y, 0);
            }

            base.Update();

            if (light == null)
            {
                light = new SpreadLight();
                LightManager.AddLight(light);
            }
            if(Input.MiddleMouseButtonDown)
                light.Position = Input.WorldMousePos;
            light.Color = Color.Beige;
            (light as SpreadLight).Radius = 30;
            light.Recalculate();
            LightRenderer.Material.BlendState = BlendState.NonPremultiplied;

            Main.Pathfinder?.Update();
        }

        #region ImGui

        private bool isPickingStart, isPickingEnd;
        private Point start, end;
        private PathResult result = PathResult.ERROR_INTERNAL;
        private int selectedDefIndex;
        private string[] entityDefNames;
        private bool spawnEntityOnClick;
        private bool liveRepath;
        private Vector2 startDrag;
        private Vector2 endDrag;
        private bool doWorldSelection;
        private bool doSpawnBullets;
        private bool linecastOnDrag;
        private readonly RaycastHit[] hits = new RaycastHit[100];

        private void DrawImGui()
        {
            #region Pathfinding
            ImGui.Begin("Pathfinding");

            if (ImGui.CollapsingHeader("Threads", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var finder = Main.Pathfinder;
                if (finder != null)
                {
                    ImGui.Text($"Thread count: {finder.ThreadCount}");
                    for (int i = 0; i < finder.ThreadCount; i++)
                    {
                        finder.GetThreadStats(i, out var times, out int timesHeader, out var usage, out var processedPerSecond, out var pNodeUsage, out var pNodePercentage, out var openNodesPercentage);
                        var usageColor = usage < 0.5 ? Color.LawnGreen.ToNumerics() : usage < 0.9 ? Color.Yellow.ToNumerics() : Color.Red.ToNumerics();
                        float lastTime = times[timesHeader];
                        var lastTimeColor = lastTime <= 1 ? Color.LawnGreen.ToNumerics() : lastTime < 16 ? Color.Yellow.ToNumerics() : Color.Red.ToNumerics();
                        var poolExcessColor = pNodePercentage < 1.0 ? Color.LawnGreen.ToNumerics() : Color.Red.ToNumerics();
                        var openNodesColor = openNodesPercentage < 0.95 ? Color.LawnGreen.ToNumerics() : Color.Red.ToNumerics();
                        ImGui.Text("Usage: ");
                        ImGui.SameLine();
                        ImGui.TextColored(usageColor, $"{usage * 100.0:F0}%%");
                        ImGui.Text($"Processed last second: {processedPerSecond}");
                        ImGui.Text("Latest path time: ");
                        ImGui.SameLine();
                        ImGui.TextColored(lastTimeColor, $"{lastTime}ms");
                        ImGui.Text("PNode usage: ");
                        ImGui.SameLine();
                        ImGui.TextColored(poolExcessColor, $"{pNodeUsage} ({pNodePercentage*100f:F0}%%)");
                        ImGui.Text("Open node usage: ");
                        ImGui.SameLine();
                        ImGui.TextColored(openNodesColor, $"{openNodesPercentage * 100f:F0}%%");
                        ImGui.PlotLines($"#{i}Path Times", ref times[0], times.Length, timesHeader, $"#{i} Times", 0, 100, new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 100));
                        ImGui.Spacing();
                        ImGui.Spacing();
                    }
                }
            }

            if (ImGui.CollapsingHeader("Test Path", ImGuiTreeNodeFlags.DefaultOpen))
            {
                #region Pick start / end
                bool isPicking = isPickingStart || isPickingEnd || liveRepath;
                ImGui.Text($"Start: {start}");
                ImGui.SameLine();
                bool pickStartClicked = false;
                NezImGui.DrawDisabledIf(isPicking, () =>
                {
                    pickStartClicked = ImGui.Button("Pick start");
                });
                if (pickStartClicked)
                {
                    isPickingStart = true;
                }
                ImGui.Text($"End: {end}");
                ImGui.SameLine();
                bool pickEndClicked = false;
                NezImGui.DrawDisabledIf(isPicking, () =>
                {
                    pickEndClicked = ImGui.Button("Pick end");
                });
                if (pickEndClicked)
                {
                    isPickingEnd = true;
                }
                ImGui.Checkbox("Live repath", ref liveRepath);
                if (isPickingStart || isPickingEnd)
                    liveRepath = false;
                #endregion

                #region Path calc

                if (ImGui.Button("Calculate") || liveRepath)
                {
                    Core.DebugRenderEnabled = true;

                    Main.Pathfinder.FindPath(start, end, (r, p, o) =>
                    {
                        result = r;
                        if (r != PathResult.SUCCESS)
                            return;

                        for (int i = 0; i < p.Count - 1; i++)
                        {
                            Vector2 here = Map.TileToWorldPosition(p[i]);
                            Vector2 next = Map.TileToWorldPosition(p[i + 1]);
                            Debug.DrawLine(here, next, Color.Green, 0);
                        }

                        Pathfinder.FreePath(p);
                    });
                }

                ImGui.Text("Latest result: ");
                ImGui.SameLine();
                ImGui.TextColored(result == PathResult.SUCCESS ? Color.Green.ToNumerics() : Color.Red.ToNumerics(), result.ToString());

                #endregion

                #region Draw gizmos
                Debug.DrawHollowBox(Map.TileToWorldPosition(start), Tile.SIZE, Color.Green);
                Debug.DrawHollowBox(Map.TileToWorldPosition(end), Tile.SIZE, Color.Red);
                #endregion
            }

            ImGui.End();

            if (isPickingStart)
            {
                start = Map.MouseTileCoordinates;
                if(Input.LeftMouseButtonDown)
                    isPickingStart = false;
            }else if (isPickingEnd || liveRepath)
            {
                end = Map.MouseTileCoordinates;
                if (Input.LeftMouseButtonDown && !liveRepath)
                    isPickingEnd = false;
            }
            #endregion

            #region EntityDef spawn

            if (entityDefNames == null)
            {
                var list = EntityDef.GetAll();
                entityDefNames = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    entityDefNames[i] = list[i].DefName;
                }
                Array.Sort(entityDefNames);
            }

            ImGui.Begin("Entity Defs");
            ImGui.ListBox("Entities", ref selectedDefIndex, entityDefNames, entityDefNames.Length, 10);
            ImGui.Checkbox("Spawn on click", ref spawnEntityOnClick);
            if (spawnEntityOnClick && Input.LeftMouseButtonPressed)
            {
                EntityDef toSpawn = EntityDef.Get(entityDefNames[selectedDefIndex]);
                if (toSpawn != null)
                {
                    var spawned = toSpawn.Create(this);
                    spawned.Position = Input.WorldMousePos;
                }
            }

            ImGui.Text($"GPU: {Core.GraphicsDevice?.Adapter?.DeviceName ?? "null"}");
            ImGui.End();

            #endregion

            #region Utils

            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("Entities"))
            {
                if (ImGui.MenuItem("Do world selection", "Ctrl+S", ref doWorldSelection))
                {
                    
                }
                if (ImGui.MenuItem("Spawn Bullets", "Ctrl+B", ref doSpawnBullets))
                {
                    
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("UI"))
            {
                if (ImGui.MenuItem("Reload UI") && Canvas != null)
                {
                    UIScaleController.RemoveCanvas(Canvas);
                    var e = Canvas.Entity;
                    e.RemoveComponent(Canvas);
                    Canvas = null;

                    CreateUI(e);
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Physics"))
            {
                if (ImGui.MenuItem("Linecast on drag", "Ctrl+L", ref linecastOnDrag))
                {
                    
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();

            if (doSpawnBullets && Input.LeftMouseButtonPressed)
            {
                float angle = Random.NextAngle();
                Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Tile.SIZE * 16;
                var ent = CreateEntity("Bullet", Input.WorldMousePos);
                ent.AddComponent(new Bullet(){Velocity = vel});
            }

            if ((doWorldSelection || linecastOnDrag) && Input.LeftMouseButtonPressed)
            {
                startDrag = Input.WorldMousePos;
            }

            if ((doWorldSelection || linecastOnDrag) && Input.LeftMouseButtonDown)
            {
                var c = Color.Green;
                c.A = 70;
                endDrag = Input.WorldMousePos;

                if (doWorldSelection)
                {
                    Debug.DrawHollowRect(new Rectangle((int)startDrag.X, (int)startDrag.Y, (int)(endDrag.X - startDrag.X), (int)(endDrag.Y - startDrag.Y)), c);
                }
                else if (linecastOnDrag)
                {
                    Debug.DrawLine(startDrag, endDrag, c);
                    int count = Physics.LinecastAll(startDrag, endDrag, hits);
                    for (int i = 0; i < count; i++)
                    {
                        var hit = hits[i];
                        Debug.DrawHollowBox(hit.Point, 3, Color.Red);
                        Debug.DrawLine(hit.Point, hit.Point + hit.Normal * Tile.SIZE / 2, Color.Purple);
                    }
                }
            }

            if ((doWorldSelection || linecastOnDrag) && Input.LeftMouseButtonReleased)
            {
                endDrag = Input.WorldMousePos;

                if (doWorldSelection)
                {
                    var found = Physics.OverlapRectangle(new RectangleF(startDrag, endDrag - startDrag));
                    if (found != null && !found.Entity.IsNullOrDestroyed())
                    {
                        Core.GetGlobalManager<ImGuiManager>().StartInspectingEntity(found.Entity);
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
