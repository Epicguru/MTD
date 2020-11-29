
//#define ALWAYS_CATCH_ERRORS

using ImGuiNET;
using JDef;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MTD.Components;
using MTD.Entities;
using MTD.Scenes;
using MTD.World;
using MTD.World.Pathfinding;
using Nez;
using Nez.BitmapFonts;
using Nez.Console;
using Nez.ImGuiTools;
using Nez.Sprites;
using Nez.Textures;
using Nez.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using JXml;
using Microsoft.Xna.Framework.Audio;
using MTD.Effects;
using MTD.Resources;
using Spriter2Nez;
using SpriterDotNet;
using SpriterDotNet.Providers;
using Debug = Nez.Debug;
using Point = Microsoft.Xna.Framework.Point;
using WindowStyle = Nez.UI.WindowStyle;

namespace MTD
{
    internal static class Program
    {
        private static void Main(string[] _)
        {
#if !DEBUG || ALWAYS_CATCH_ERRORS
            string genCrashReport(Exception e)
            {
                return e.ToString();
            }

            try
            {
#endif
                using Main g = new Main();
                g.Run();
#if !DEBUG || ALWAYS_CATCH_ERRORS
            }
            catch (Exception e)
            {
                try
                {
                    string folder = "./Crash Reports/";
                    string name = DateTime.UtcNow.ToString("s-m-HH--d-MM-yy");
                    string fileName = folder + name + ".txt";
                    string data = genCrashReport(e);
                    new FileInfo(fileName).Directory.Create();
                    File.WriteAllText(fileName, data);
                }
                catch(Exception e2)
                {
                    Debug.Error(e2.ToString());
                }
                
                MessageBox.Show($"Uh oh! The game has crashed. Error:\n{e}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1); // Force shutdown threads.
            }
#endif
        }
    }

    public class Main : Core
    {
        public const int LAYER_UI = 999;
        public const int LAYER_LIGHT = 998;
        public const int LAYER_MOTES = 50;
        public const int LAYER_TILES = 10;

        public static bool DebugStandableTiles = false;

        public static DefDatabase Defs { get; private set; }
        public static SpriteAtlas Atlas { get; private set; }
        public static SpriteAtlas UIAtlas { get; private set; }

        public static BitmapFont Font24 { get; private set; }
        public static BitmapFont Font32 { get; private set; }
        public static BitmapFont FontTitle { get; private set; }

        public static int PathfindingThreadCount { get; set; } = 4;
        public static Pathfinder Pathfinder { get; internal set; }

        private static ThreadController _threadController;

        private bool hasExited = false;

        public static void PostToMainThread(Action a)
        {
            _threadController?.Post(a);
        }

        protected override void Initialize()
        {
            base.Initialize();
            Window.AllowUserResizing = true;
            CreateImGuiManager();
            DebugConsole.RenderScale = 2;

            RegisterGlobalManager(new UIScaleController());
            RegisterGlobalManager(_threadController = new ThreadController());

            Scene = CreateLoadCoreAssetsScene();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            if (hasExited)
                return;
            hasExited = true;

            base.OnExiting(sender, args);
            Pathfinder?.Dispose();
            try
            {
                Scene?.Unload();
            }
            catch (Exception e)
            {
                Debug.Error(e.ToString());
            }
        }

        private LoadingScene CreateLoadCoreAssetsScene()
        {
            var ls = new LoadingScene();
            ls.Load = () =>
            {
                var sw = new Stopwatch();
                sw.Start();

                ls.SetMessage("Loading core content...");
                LoadEssentialContent();
                PackMainAtlas(ls);

                sw.Stop();
                Debug.Log("Took {0} ms to do core asset load", sw.ElapsedMilliseconds);

            };
            ls.LoadDone = () =>
            {
                Scene = CreateLoadGeneralAssetsScene();
            };
            ls.LoadError = (s, e) =>
            {
                Debug.Error("Due to core asset loading exception, game will close.");
                Exit();
            };
            return ls;
        }

        private LoadingScene CreateLoadGeneralAssetsScene()
        {
            var ls = new LoadingScene();
            ls.Load = () =>
            {
                var sw = new Stopwatch();
                sw.Start();

                LoadGeneralContent(ls);

                sw.Stop();
                Debug.Log("Took {0} ms to do general asset load", sw.ElapsedMilliseconds);
            };
            ls.LoadDone = () =>
            {
                StartSceneTransition(new FadeTransition(() => new MyScene()) { FadeOutDuration = 0.4f, FadeInDuration = 0.4f });
            };
            ls.LoadError = (s, e) =>
            {
                Debug.Error("Due to core (general) asset loading exception, game will close.");
                Exit();
            };

            return ls;
        }

        private void CreateImGuiManager()
        {
            var manager = new ImGuiManager();
            RegisterGlobalManager(manager);
            manager.ShowSeperateGameWindow = false;
            try
            {
                NezImGuiThemes.PhotoshopDark();
                var style = ImGui.GetStyle();
                style.FramePadding.Y = 4;
                style.Colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0f, 237 / 255f, 1f, 130 / 255f);
                style.WindowTitleAlign.X = 0.5f;
            }
            catch (Exception e)
            {
                Debug.Error(e.ToString());
            }
            manager.SetEnabled(false);
        }

        private void PackMainAtlas(LoadingScene ls)
        {
            bool worked = RuntimePacker.PackAll("Content/", "Content/MainAtlas", (step) =>
            {
                ls.SetMessage($"Packing sprites: {step}");
            });
            if (!worked)
                throw new Exception("Atlas packing failed.");
        }

        private void LoadEssentialContent()
        {
            FontTitle = Content.LoadBitmapFont("Content/Fonts/Title72.fnt");
        }

        private void LoadGeneralContent(LoadingScene ls)
        {
            #region Main font, atlases...

            ls.SetMessage("Loading main fonts...");
            Font32 = Content.LoadBitmapFont("Content/Fonts/General32.fnt");
            Font24 = Content.LoadBitmapFont("Content/Fonts/General24.fnt");

            ls.SetMessage("Loading main atlas...");
            Atlas = Content.LoadSpriteAtlas("Content/MainAtlas.atlas", true);

            ls.SetMessage("Loading ui atlas...");
            UIAtlas = Content.LoadSpriteAtlas("Content/UI.atlas", true);

            #endregion

            #region Animation framework

            // Only needs doing once.
            var factory = new DefaultProviderFactory<Sprite, SoundEffect>(new Config
            {
                MetadataEnabled = false,
                EventsEnabled = true,
                PoolingEnabled = true,
                TagsEnabled = false,
                VarsEnabled = false,
                SoundsEnabled = false
            }, true);
            NezAnimator.DefaultProviderFactory = factory;

            #endregion

            #region Load Defs

            // Custom type names.
            bool CompilerGen(Type t)
            {
                var attr = Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute));
                return attr != null;
            }
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsClass && !CompilerGen(type))
                { 
                    TypeResolver.CustomTypeNames.Add(type.Name, type);
                }
            }

            // Load defs!
            Defs = new DefDatabase();
            Defs.AddCustomResolver(typeof(Sprite), (args) =>
            {
                string path = args.XmlNode.InnerText;
                SpriteAtlas atlas = Main.Atlas;

                var customAtlasTag = args.XmlNode.Attributes.GetNamedItem("atlas");
                if (customAtlasTag != null)
                {
                    string name = customAtlasTag.InnerText.ToLowerInvariant();
                    if(name == "ui")
                        atlas = Main.UIAtlas;
                }
                var sprite = atlas.GetSprite(path);
                if (sprite == null)
                    Debug.Error("Failed to find sprite for path '{0}' in atlas.", path);
                return sprite;
            });

            ls.SetMessage("Loading defs...");
            Defs.LoadFromDir("Content/Defs/");

            ls.SetMessage("Processing defs...");
            Defs.Process();

            #endregion

            #region Specific Def Load (TileDef, EntityDef etc.)

            // Load specific classes of defs.
            ls.SetMessage("Sorting defs...");
            TileDef.Load();
            EntityDef.Load();
            ProjectileDef.Load();
            MoteDef.Load();
            ResourceDef.Load();

            #endregion
        }

        private static Point lastSize;
        [Command("toggle-fullscreen", "Changes between fullscreen mode and windowed mode.")]
        private static void ToggleFullscreen()
        {
            if (lastSize == Point.Zero)
                lastSize = new Point(Screen.MonitorWidth / 2, Screen.MonitorHeight / 2);

            if (Screen.IsFullscreen)
            {
                Screen.IsFullscreen = false;
                Screen.SetSize(lastSize.X, lastSize.Y);
            }
            else
            {
                lastSize = new Point(Screen.Width, Screen.Height);
                Screen.IsFullscreen = true;
                Screen.SetSize(Screen.MonitorWidth, Screen.MonitorHeight);
            }
        }

        [Command("toggle-borderless", "Toggles the window's borderless state.")]
        private static void ToggleBorderless()
        {
            Instance.Window.IsBorderlessEXT = !Instance.Window.IsBorderlessEXT;
        }

        [Command("clear-entities", "Destroys all entities, apart from the scene cameras.")]
        private static void ClearEntities()
        {
            var scene = Scene;
            if (scene == null)
                return;

            var list = scene.Entities.RootEntities();
            foreach (var e in list)
            {
                var cam = e.GetComponentInChildren<Camera>();
                if (cam == null)
                    e.Destroy();
            }
            ListPool<Entity>.Free(list);
            DebugConsole.Instance.Log("Cleared entities.");
        }

        [Command("light", "Toggles light on or off.")]
        private static void ToggleLight()
        {
            var gs = Scene as GameScene;
            if (gs == null)
                return;

            gs.LightPostProcessor.Enabled = !gs.LightPostProcessor.Enabled;
        }

        [Command("light-color", "Sets light ambient color.")]
        private static void LightColor(byte r, byte g, byte b)
        {
            var gs = Scene as GameScene;
            if (gs == null)
                return;

            Color c = Color.Create(r, g, b, 0);
            gs.LightRenderer.RenderTargetClearColor = c;
        }

        [Command("light-debug", "Sets light ambient color.")]
        private static void LightDebug()
        {
            var gs = Scene as GameScene;
            if (gs == null)
                return;

            gs.LightPostProcessor.DoDebugInspect = !gs.LightPostProcessor.DoDebugInspect;
        }

        [Command("toggle-stand-debug", "Toggles the standable tiles debug overlay.")]
        private static void ToggleStandableDebug()
        {
            DebugStandableTiles = !DebugStandableTiles;
        }
    }

    public class UIScaleController : GlobalManager
    {
        public static float Scale { get; set; } = 1;

        private static readonly List<UICanvas> canvases = new List<UICanvas>();

        public static void RegisterCanvas(UICanvas c)
        {
            if (c == null || canvases.Contains(c))
                return;

            canvases.Add(c);
        }

        public static void RemoveCanvas(UICanvas c)
        {
            if (c == null || !canvases.Contains(c))
                return;

            canvases.Remove(c);
        }

        public override void Update()
        {
            Input.SetMouseOverImGui(ImGui.IsAnyItemHovered());

            for (int i = 0; i < canvases.Count; i++)
            {
                var c = canvases[i];
                if (c == null || (c.Entity?.IsDestroyed ?? true) || (c.Stage?.GetRoot() == null))
                {
                    Debug.Error("A canvas was destroyed but was not removed from the UIScaleController! Remember to call UIScaleController.RemoveCanvas()");
                    canvases.RemoveAt(i);
                    i--;
                    continue;
                }

                var root = c.Stage.GetRoot();
                root.SetOrigin(0, 0);
                root.SetScale(Scale);
            }

            base.Update();
        }

        [Command("ui-debug", "Toggles all canvas ui debug rendering on or off.")]
        private static void ToggleDebugMouse()
        {
            bool toToggleTo = false;
            bool hasDecidedToggle = false;
            foreach (var c in canvases)
            {
                if (c == null || c.Entity.IsNullOrDestroyed() || c.Stage == null)
                    continue;

                if (!hasDecidedToggle)
                {
                    hasDecidedToggle = true;
                    toToggleTo = !c.Stage.GetDebugAll();
                }

                c.Stage.SetDebugAll(toToggleTo);
            }
        }

        [Command("ui-scale", "Get or set the ui scale.")]
        private static void SetUIScale(float scale)
        {
            if (scale <= 0)
            {
                // Get.
                DebugConsole.Instance.Log($"UI scale is {Scale*100:F1}%\nThere are currently {canvases.Count} canvases being scaled.");
            }
            else
            {
                // Set.
                float old = Scale;
                Scale = scale;
                DebugConsole.Instance.Log($"UI scale updated: {old * 100f:F0}% -> {Scale * 100:F0}%");
            }
        }
    }

    public class ThreadController : GlobalManager
    {
        public int QueuedItems
        {
            get
            {
                return todo.Count;
            }
        }

        private readonly ConcurrentQueue<Action> todo = new ConcurrentQueue<Action>();

        public void Post(Action a)
        {
            if (a == null)
                return;

            todo.Enqueue(a);
        }

        public override void Update()
        {
            base.Update();

            while (todo.TryDequeue(out var toRun))
            {
                toRun?.Invoke();
            }
        }
    }

    class MyScene : Scene
    {
        private UICanvas ui;

        public override void Initialize()
        {
            base.Initialize();

            SamplerState = SamplerState.PointClamp;
            AddRenderer(new RenderLayerExcludeRenderer(0, 999)); // For world objects.
            AddRenderer(new ScreenSpaceRenderer(100, 999)); // For UI.
        }

        public override void OnStart()
        {
            base.OnStart();

            // Register ImGUI drawer..
            var manager = Core.GetGlobalManager<ImGuiManager>();
            manager.RegisterDrawCommand(DrawSomeUI);

            // Create UI.
            SetupMenu();
            UIScaleController.RegisterCanvas(ui);

            // For every spriter project...
            var loader = Content.LoadAnimationProject("Anim/Motes/Motes");

            // For every renderer. (each renderer needs it's own animator)
            CreateEntity("Animator").AddComponent(new AnimationRenderer(loader.GetEntity("Pick")));
            var e = CreateEntity("Animator2");

            e.AddComponent(new AnimationRenderer(loader.GetEntity("Pick")));
            e.Position = new Vector2(200, 200);
        }

        public override void Unload()
        {
            base.Unload();
            UIScaleController.RemoveCanvas(ui);
            ui = null;
        }

        public override void Update()
        {
            Vector2 vel = new Vector2();
            if (Input.IsKeyDown(Keys.A))
                vel.X -= 1;
            if (Input.IsKeyDown(Keys.D))
                vel.X += 1;
            if (Input.IsKeyDown(Keys.W))
                vel.Y -= 1;
            if (Input.IsKeyDown(Keys.S))
                vel.Y += 1;

            vel.Normalize();
            vel *= Time.UnscaledDeltaTime * Tile.SIZE * 10f;

            Camera.Position += vel;

            base.Update();
        }

        private void SetupMenu()
        {
            ui = CreateEntity("UI").AddComponent(new UICanvas());
            ui.SetRenderLayer(999);
            ui.IsFullScreen = true;

            var skin = Skin.CreateDefaultSkin();
            var font = Main.Font32;
            skin.Get<LabelStyle>().Font = font;
            skin.Get<TextButtonStyle>().Font = font;
            skin.Get<WindowStyle>().TitleFont = font;
            var table = ui.Stage.AddElement(new Table());

            table.SetFillParent(true).Center();

            var playTestButton = table.Add(new TextButton("Play Test Level", skin)).SetFillX().SetMinHeight(30).GetElement<TextButton>();
            playTestButton.OnClicked += b =>
            {
                Debug.Log("Clicked!");
                ui.ShowDialog("Some title", "Some message", "Close", skin);
            };

            table.Row().SetPadTop(15f);

            var exitButton = table.Add(new TextButton("Transition", skin)).SetFillX().SetMinHeight(30).GetElement<TextButton>();
            exitButton.OnClicked += b =>
            {
                //Core.Exit();
                var gs = new GameScene();
                LoadingScene.LoadAndChangeScene(gs, () =>
                {
                    var map = new Map(1000, 200);
                    GameScene.GenerateMap(map);
                    gs.Map = map;
                });
            };

            table.Row().SetPadTop(15);

            table.Add(new Label("Some text here.", skin));

            var newTable2 = new Table().SetFillParent(true).Left().Bottom();
            var thing = new HorizontalGroup(10);
            thing.SetAlignment(Align.Bottom);
            thing.AddElement(new TextButton("Hey", skin));
            thing.AddElement(new TextButton("Ho", skin));
            newTable2.Add(thing);
            ui.Stage.AddElement(newTable2);

            var newTable = new Table();
            newTable.Pad(10).SetFillParent(true).Right().Top();
            newTable.Add(new TextButton("Heya", skin));
            ui.Stage.AddElement(newTable);
        }

        private void DrawSomeUI()
        {
            var size = base.SceneRenderTargetSize;
            ImGui.Begin("My custom window");
            ImGui.Text($"Current scene: {Core.Scene?.GetType().Name ?? "null"}");
            ImGui.Text($"Current renderer count: {Core.Scene?.RendererCount ?? 0}");
            ImGui.Text($"Current post renderer count: {Core.Scene?.PostRendererCount ?? 0}");
            ImGui.Text($"Display: {Screen.MonitorWidth}x{Screen.MonitorHeight}");
            ImGui.Text($"Screen: {Screen.Width}x{Screen.Height}");
            ImGui.Text($"RTS: {size.X}x{size.Y}");
            ImGui.Text($"UI: {ui?.Width}x{ui?.Height}");
            ImGui.Text($"World cam bounds: {Camera?.Bounds}");
            if (ImGui.Button("Go fullscreen"))
            {
                if (!Screen.IsFullscreen)
                {
                    Debug.Log("Going fullscreen, {0}x{1}", Screen.MonitorWidth, Screen.MonitorHeight);
                    Screen.IsFullscreen = true;
                    Screen.SetSize(Screen.MonitorWidth, Screen.MonitorHeight);
                }
                else
                {
                    Screen.IsFullscreen = false;
                    Screen.SetSize(Screen.MonitorWidth / 2, Screen.MonitorHeight / 2);
                }
            }
            if (ImGui.Button("Screenshot"))
            {
                base.RequestScreenshot((tex) =>
                {
                    //var tex = base.SceneRenderTarget;
                    using var fs = new System.IO.FileStream(@"C:\Users\spain\Desktop\Screen.png", FileMode.OpenOrCreate);
                    tex.SaveAsPng(fs, tex.Width, tex.Height);
                });
            }

            ImGui.Spacing();

            ImGui.Text($"World Mouse Pos: {Input.WorldMousePos}");
            ImGui.End();
        }
    }

    public class AnotherScene : Scene
    {
        public override void OnStart()
        {
            base.OnStart();
            var manager = Core.GetGlobalManager<ImGuiManager>();
            manager.RegisterDrawCommand(DrawSomeUI);
        }

        private void DrawSomeUI()
        {
            ImGui.Begin("Another custom window");
            ImGui.LabelText("Label:", "Hello, world! Version 2!");
            ImGui.End();
        }
    }

    public class CustomTextRenderer : RenderableComponent
    {
        public override float Width => 100;
        public override float Height => 100;
        public BitmapFont Font;
        public char Char;
        public CustomTextRenderer(BitmapFont font)
        {
            this.Font = font;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            Character currentChar = Font.Characters[Char];
            var dest = new Rectangle((int)Transform.Position.X, (int)Transform.Position.Y, currentChar.Bounds.Width, currentChar.Bounds.Height);
            batcher.DrawRect(dest, Color.White);
            batcher.Draw(Font.Textures[currentChar.TexturePage], dest, currentChar.Bounds, Color.Red);
        }
    }
}
