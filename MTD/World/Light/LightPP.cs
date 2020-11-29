using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Textures;

namespace MTD.World.Light
{
    public class LightPP : PostProcessor
    {
        public bool DoDebugInspect = false;
        public float BlurRadius = 64;

        private readonly float[] kernels = {
            0.024626f,
            0.027624f,
            0.03068f,
            0.033735f,
            0.036725f,
            0.039583f,
            0.042239f,
            0.044625f,
            0.046677f,
            0.048338f,
            0.049561f,
            0.050309f,
            0.050561f,
            0.050309f,
            0.049561f,
            0.048338f,
            0.046677f,
            0.044625f,
            0.042239f,
            0.039583f,
            0.036725f,
            0.033735f,
            0.03068f,
            0.027624f,
            0.024626f
        };

        private RenderTexture rt;

        public LightPP(int executionOrder) : base(executionOrder)
        {
            rt = new RenderTexture(Screen.Width, Screen.Height, DepthFormat.None);
            rt.ResizeBehavior = RenderTexture.RenderTextureResizeBehavior.SizeToSceneRenderTarget;
        }

        public RenderTexture GetRenderTexture()
        {
            return rt;
        }

        public override void OnAddedToScene(Scene scene)
        {
            base.OnAddedToScene(scene);

            Effect  = scene.Content.LoadEffect("Shaders/LightBlur.fxb");
        }

        public override void Unload()
        {
            rt.Dispose();
            rt = null;

            base.Unload();
        }

        private RenderTarget2D GetTempRT()
        {
            return RenderTarget.GetTemporary(rt.RenderTarget.Width, rt.RenderTarget.Height, DepthFormat.None);
        }

        public override void Process(RenderTarget2D source, RenderTarget2D destination)
        {
            if (DoDebugInspect)
            {
                // Draw light RT straight to screen.
                DrawFullscreenQuad(rt, destination);
                return;
            }

            SamplerState = SamplerState.LinearClamp;

            // TODO cache parameters
            Effect.Parameters["weights"].SetValue(kernels);
            Effect.Parameters["radius"].SetValue(BlurRadius * _scene?.Camera?.RawZoom ?? 1);
            Effect.Parameters["sceneTex"].SetValue(source); // Set to unlit scene texture.
            var tempRT = GetTempRT();

            #region Horizontal Blur

            Effect.Parameters["axis"].SetValue(new Vector3(1f, 0f, 0f));
            Effect.Parameters["resolution"].SetValue((float)rt.RenderTarget.Width);

            // Draw light rt to a new temp rt.
            DrawFullscreenQuad(rt, tempRT, Effect); // Src: un-blurred light, dst: blank rt
            #endregion

            #region Vertical Blur

            Effect.Parameters["axis"].SetValue(new Vector3(0f, 1f, 1f));
            Effect.Parameters["resolution"].SetValue((float)rt.RenderTarget.Height);

            // Draw light rt to a new temp rt.
            DrawFullscreenQuad(tempRT, destination, Effect); // Src: horizontally blurred, dst: output, other: unlit scene

            #endregion

            RenderTarget.ReleaseTemporary(tempRT);
        }
    }
}
