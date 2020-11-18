﻿using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MTD.Entities;
using MTD.World;
using MTD.World.Pathfinding;
using Nez;
using Nez.ImGuiTools;
using Nez.Shadows;
using System;
using Random = Nez.Random;

namespace MTD.Scenes
{
    public class GameScene : Scene
    {
        public static void GenerateLayer(TileLayer layer)
        {
            int w = layer.WidthInTiles;
            int h = layer.HeightInTiles;

            var dirt = Main.Defs.GetNamed<TileDef>("DirtTile");
            var stone = Main.Defs.GetNamed<TileDef>("StoneTile");

            int height = h / 2;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (y >= h - height / 2)
                        layer.SetTile(x, y, stone, true);
                    else if (y >= h - height)
                        layer.SetTile(x, y, dirt, true);

                }
                if (Random.Chance(0.3f))
                    height += Random.Range(-1, 2);
            }
        }

        public Map Map { get; internal set; }
        public UICanvas Canvas { get; private set; }

        private PolyLight light;

        public override void Initialize()
        {
            base.Initialize();

            AddRenderer(new DefaultRenderer());
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
            mapEnt.AddComponent(Map);

            Core.GetGlobalManager<ImGuiManager>().RegisterDrawCommand(DrawImGui);

            CreateUI(CreateEntity("UI"));
        }

        public void CreateUI(Entity target)
        {
            Canvas = GameSceneUI.Setup(this, Main.UIAtlas);
            target.AddComponent(Canvas);
            UIScaleController.RegisterCanvas(Canvas);
        }

        public override void Unload()
        {
            base.Unload();

            UIScaleController.RemoveCanvas(Canvas);
            Canvas = null;

            var path = Main.Pathfinder;
            Main.Pathfinder = null;
            path.Dispose();
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

            base.Update();

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

            ImGui.End();

            #endregion

            #region Entity Utils

            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("Entities"))
            {
                if (ImGui.MenuItem("Do world selection", "Ctrl+S", ref doWorldSelection))
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
            ImGui.EndMainMenuBar();

            if (doWorldSelection && Input.LeftMouseButtonPressed)
            {
                startDrag = Input.WorldMousePos;
            }

            if (doWorldSelection && Input.LeftMouseButtonDown)
            {
                var c = Color.Green;
                c.A = 70;
                endDrag = Input.WorldMousePos;
                Debug.DrawHollowRect(new Rectangle((int)startDrag.X, (int)startDrag.Y, (int)(endDrag.X - startDrag.X), (int)(endDrag.Y - startDrag.Y)), c);
            }

            if (doWorldSelection && Input.LeftMouseButtonReleased)
            {
                endDrag = Input.WorldMousePos;
                var found = Physics.OverlapRectangle(new RectangleF(startDrag, endDrag - startDrag));
                if (found != null && !found.Entity.IsNullOrDestroyed())
                {
                    Core.GetGlobalManager<ImGuiManager>().StartInspectingEntity(found.Entity);
                }
            }

            #endregion
        }

        #endregion
    }
}
