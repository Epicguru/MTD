using Microsoft.Xna.Framework;
using Nez;
using Nez.ImGuiTools;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ImGuiNET;
using Nez.Textures;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez.BitmapFonts;
using Nez.UI;
using Nez.Console;
using Nez.Analysis;

namespace FnaTest
{
    class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine("Hello World!");
            using Game g = new MyGame();
            g.Run();
        }
    }

    class MyGame : Core
    {
        protected override void Initialize()
        {
            base.Initialize();

            Window.AllowUserResizing = true;

            var manager = new ImGuiManager();
            RegisterGlobalManager(manager);
            manager.ShowSeperateGameWindow = false;
            try
            {
                NezImGuiThemes.PhotoshopDark();
                var style = ImGui.GetStyle();
                style.FramePadding.Y = 4;
                style.Colors[(int) ImGuiCol.Border] = new System.Numerics.Vector4(0f, 237 / 255f, 1f, 130 / 255f);
                style.WindowTitleAlign.X = 0.5f;
            }
            catch (Exception e)
            {
                Debug.Error(e.ToString());
            }
            manager.SetEnabled(false);

            RegisterGlobalManager(new UIScaleController());

            DebugConsole.RenderScale = 2;

            Scene = new MyScene();
        }

        private static Point lastSize;
        [Command("toggle-fullscreen", "Changes between fullscreen mode and windowed mode.")]
        public static void ToggleFullscreen()
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
        public static void ToggleBorderless()
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
    }

    class UIScaleController : GlobalManager
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

    class MyScene : Scene
    {
        private UICanvas ui;

        public override void Initialize()
        {
            base.Initialize();

            SamplerState = SamplerState.PointClamp;
            AddRenderer(new RenderLayerExcludeRenderer(0, 999)); // For UI.
            AddRenderer(new ScreenSpaceRenderer(100, 999)); // For everything else.
        }

        public override void OnStart()
        {
            base.OnStart();
            var manager = Core.GetGlobalManager<ImGuiManager>();
            manager.RegisterDrawCommand(DrawSomeUI);

            bool worked = RuntimePacker.Pack("./Content/", "./Content/MainAtlas", (step) =>
            {
                Debug.Log(step);
            });
            if (!worked)
                throw new Exception("Atlas packing failed.");

            SetupMenu();
            UIScaleController.RegisterCanvas(ui);

            var atlas = Content.LoadSpriteAtlas("Content/MainAtlas.atlas");

            CreateEntity("BG").AddComponent(new SpriteRenderer(atlas.GetSprite("BG"))).RenderLayer = 1;
            CreateEntity("Ball").AddComponent(new SpriteRenderer(atlas.GetSprite("Face")));
        }

        public override void Unload()
        {
            base.Unload();
            UIScaleController.RemoveCanvas(ui);
            ui = null;
        }

        private void SetupMenu()
        {
            ui = CreateEntity("UI").AddComponent(new UICanvas());
            ui.SetRenderLayer(999);
            ui.IsFullScreen = true;

            var skin = Skin.CreateDefaultSkin();
            var font = Content.LoadBitmapFont("Content/Fonts/MyFont.fnt");
            skin.Get<LabelStyle>().Font = font;
            skin.Get<TextButtonStyle>().Font = font;
            skin.Get<WindowStyle>().TitleFont = font;
            var table = ui.Stage.AddElement(new Table());

            //var e = CreateEntity("Test");
            //e.Position = Screen.Center + new Vector2(300, 0);
            //e.AddComponent(new SpriteRenderer(new Sprite(font.Textures[0])));

            //CreateEntity("Text").AddComponent(new TextComponent(font, "Some Text", Vector2.Zero, Color.White));
            //CreateEntity("Custom Char Renderer").AddComponent(new CustomTextRenderer(font) { Char = 'T' });

            table.SetFillParent(true).Center();

            var playTestButton = table.Add(new TextButton("Play Test Level", skin)).SetFillX().SetMinHeight(30).GetElement<TextButton>();
            playTestButton.OnClicked += b =>
            {
                Debug.Log("Clicked!");
                ui.ShowDialog("Some title", "Some message", "Close", skin);
            };

            table.Row().SetPadTop(15f);

            var exitButton = table.Add(new TextButton("Exit", skin)).SetFillX().SetMinHeight(30).GetElement<TextButton>();
            exitButton.OnClicked += b =>
            {
                Core.Exit();
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
