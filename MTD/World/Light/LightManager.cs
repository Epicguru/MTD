using System;
using Nez;
using System.Collections.Generic;

namespace MTD.World.Light
{
    public class LightManager : RenderableComponent, IDisposable
    {
        public override RectangleF Bounds => Map.Current.Bounds;

        private List<Light> lights = new List<Light>();

        public LightManager()
        {
            base.RenderLayer = Main.LAYER_LIGHT;
        }

        public Light AddLight(Light l)
        {
            if (l == null)
                return null;

            if (!lights.Contains(l))
                lights.Add(l);

            return l;
        }

        public void RemoveLight(Light l)
        {
            if (l == null)
                return;
            if (lights.Contains(l))
                lights.Remove(l);
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            foreach (var light in lights)
            {
                if (light == null || !light.Enabled)
                    continue;

                light.Render(batcher, camera);
            }
        }

        public void Dispose()
        {
            lights.Clear();
            lights = null;
        }
    }
}
