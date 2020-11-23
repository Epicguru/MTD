using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Textures;

namespace MTD.World
{
    public class LightPP : PostProcessor
    {
        public bool DoLight = true;
        public float BlurRadius = 64;

        private Vector2[] offsets;

        private RenderTexture rt;

        public LightPP(int executionOrder, RenderTexture rt) : base(executionOrder)
        {
            this.rt = rt;
            CreateOffsets(32);
        }

        private void CreateOffsets(int dirs)
        {
            offsets = new Vector2[dirs];
            for (int i = 0; i < dirs; i++)
            {
                float p = (float) i / (dirs - 1);
                float a = (float)(p * Math.PI * 2.0);
                offsets[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
        }

        public override void OnAddedToScene(Scene scene)
        {
            base.OnAddedToScene(scene);

            Effect = scene.Content.LoadEffect("Shaders/LightShader.fxb");
        }

        public override void Unload()
        {
            rt = null;

            base.Unload();
        }

        public override void Process(RenderTarget2D source, RenderTarget2D destination)
        {
            // TODO cache parameters
            Effect.Parameters["inputTex"].SetValue(rt);
            Effect.Parameters["resolution"].SetValue(new Vector2(rt.RenderTarget.Width, rt.RenderTarget.Height));
            Effect.Parameters["radius"].SetValue(BlurRadius * _scene.Camera.RawZoom);
            Effect.Parameters["offsets"].SetValue(offsets);
            //Effect.Parameters["quality"].SetValue((float)BlurQuality);
            //Effect.Parameters["directions"].SetValue((float)BlurDirections);

            // Blurs the input light texture and combines with unlit scene.
            SamplerState = SamplerState.PointClamp;
            DrawFullscreenQuad(source, destination, DoLight ? Effect : null);
        }
    }
}
