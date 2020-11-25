using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Threading;

namespace MTD.Scenes
{
    public class LoadingScene : Scene
    {
        public static void LoadAndChangeScene(Scene newScene, Action load)
        {
            if (newScene == null)
            {
                Debug.Error("NewScene == null");
                return;
            }
            if (Core.Scene is LoadingScene)
            {
                Debug.Error("Already in loading scene.");
                return;
            }
            if (load == null)
            {
                Debug.Error("Load action is null.");
                return;
            }

            var ls = new LoadingScene();
            ls.Load = load;
            ls.LoadDone = () =>
            {
                Core.StartSceneTransition(new FadeTransition(() => newScene) { FadeOutDuration = 0.4f, FadeInDuration = 0.4f });
            };
            ls.LoadError = (s, e) =>
            {
                // TODO handle this a bit better.
            };

            Core.StartSceneTransition(new FadeTransition(() => ls){ FadeOutDuration = 0.4f, FadeInDuration = 0.4f });
        }

        public Action Load;
        public Action LoadDone;
        public Action<string, Exception> LoadError;

        private string message = "LOADING";
        private bool coreAssetsLoaded;
        private TextComponent title;

        public override void Initialize()
        {
            base.Initialize();

            base.ClearColor = Color.Black;
            base.AddRenderer(new DefaultRenderer());
        }

        public override void OnStart()
        {
            base.OnStart();

            if (Load == null)
            {
                Debug.Error("Load action is null! Returning to menu screen...");
                LoadError?.Invoke("Load action is null", null);
                return;
            }

            coreAssetsLoaded = Main.FontTitle != null;

            // Version with fancy graphics
            if(coreAssetsLoaded)
                CreateLoadingIcon();
            CreateLoadingTitle();

            StartLoadingThread();
        }

        private void CreateLoadingIcon()
        {
            var ent = CreateEntity("LoadingIcon");
            var animator = ent.AddComponent(new SpriteAnimator());

            var anim = Content.LoadSpriteAtlas("Content/[Atlas]UI/LoadingIcon.atlas").CreateAnimation("LoadingIcon", 24, 30, 2, new Vector2(0.5f, 0f));

            animator.AddAnimation("Spin", anim);
            animator.Play("Spin");

            ent.Parent = Camera.Transform; // Parent to camera.
            ent.LocalPosition = Vector2.Zero; // Place right under camera.
        }

        private void CreateLoadingTitle()
        {
            var titleEnt = CreateEntity("Title");
            Color color = Color.Cyan.Multiply(new Color(0.9f, 0.9f, 0.9f, 1f));
            if (!coreAssetsLoaded)
                color = Color.White;
            IFont font = coreAssetsLoaded ? Main.FontTitle : Graphics.Instance.BitmapFont;
            title = titleEnt.AddComponent(new TextComponent(font, message, Vector2.Zero, color));
            titleEnt.Parent = Camera.Transform;
            title.SetHorizontalAlign(HorizontalAlign.Center);
            titleEnt.LocalPosition = new Vector2(0, coreAssetsLoaded ? -65 : 0);
            if (!coreAssetsLoaded)
                titleEnt.LocalScale = Vector2.One * 2;
        }

        public void SetMessage(string txt)
        {
            message = txt;
            if(title != null)
                title.Text = txt;
        }

        private void StartLoadingThread()
        {
            Thread thread = new Thread(RunLoading);
            thread.Name = "Loading Thread";
            thread.Priority = ThreadPriority.AboveNormal;

            thread.Start();
        }

        private void RunLoading()
        {
            try
            {
                Load?.Invoke();

                // Wait until transition in is done.
                while (Core.IsInTransition)
                {
                    Thread.Sleep(10);
                }

                Main.PostToMainThread(LoadDone);
            }
            catch (Exception e)
            {
                Debug.Error("Exception in loading thread:\n{0}", e);
                Main.PostToMainThread(() =>
                {
                    LoadError?.Invoke("Exception in loading thread.", e);
                });
            }
        }
    }
}
