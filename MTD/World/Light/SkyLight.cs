using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Nez;

namespace MTD.World.Light
{
    /// <summary>
    /// Handles drawing world light that comes down from the sky.
    /// In order to know what height to draw light to, it must check each tile from the sky downwards.
    /// It does this on another thread, to avoid killing framerate.
    /// </summary>
    public class SkyLight : IDisposable
    {
        private const int TARGET_TIME_MS = 16; // Attempt to process every 16ms (60 times per second)
        public readonly int WorldWidthInTiles;
        public Color LightColor = Color.White;

        private float[] heights;
        private bool run;
        private bool isRunning;

        public SkyLight(int worldWidth)
        {
            WorldWidthInTiles = worldWidth;
            heights = new float[worldWidth];
        }

        public void Start()
        {
            if (run)
            {
                Debug.Error("SkyLight is already running.");
                return;
            }
            run = true;
            isRunning = true;

            Thread t = new Thread(ThreadRun);
            t.Priority = ThreadPriority.BelowNormal;
            t.Name = "Sky Light Processor";
            t.Start();
        }

        /// <summary>
        /// Shuts down the processing thread and blocks the calling thread until the processing thread has completely stopped.
        /// </summary>
        public void Dispose()
        {
            run = false;
            while (isRunning)
            {
                Thread.Sleep(1);
            }
        }

        public void Render(Batcher batcher, Camera camera)
        {
            const int PAD = 2;
            var map = Map.Current;
            int sx = map.WorldToTileCoordinates(camera.Bounds.Location).X - PAD;
            int ex = map.WorldToTileCoordinates(camera.Bounds.Max).X + PAD;
            sx = Math.Clamp(sx, 0, map.WidthInTiles);
            ex = Math.Clamp(ex, 0, map.WidthInTiles);

            for (int x = sx; x < ex; x++)
            {
                float dx = (x - 1) * Tile.SIZE - Tile.SIZE / 2;
                const float dy = Tile.SIZE / -2f;
                const float dw = Tile.SIZE * 3;
                float dh = heights[x] * Tile.SIZE;

                batcher.DrawRect(dx, dy, dw, dh, LightColor);
            }
        }

        private void ThreadRun()
        {
            var sw = new System.Diagnostics.Stopwatch();
            
            while (run)
            {
                sw.Restart();
                var map = Map.Current;
                if (map == null)
                    continue; // This happens for a few ms upon map load due to Map.Current being assigned slightly after GameScene init.

                const int PAD = 2;
                int sx = map.WorldToTileCoordinates(Core.Scene.Camera.Bounds.Location).X - PAD;
                int ex = map.WorldToTileCoordinates(Core.Scene.Camera.Bounds.Max).X + PAD;
                int h = map.HeightInTiles;
                sx = Math.Clamp(sx, 0, map.WidthInTiles);
                ex = Math.Clamp(ex, 0, map.WidthInTiles);

                for (int x = sx; x < ex; x++)
                {
                    int y;
                    for (y = 0; y < h; y++)
                    {
                        var tile = map.GetTile(x, y, 0);
                        if (tile == null)
                            continue;

                        if (tile.BlocksLight)
                        {
                            y++; // Include the tile that blocks light.
                            break;
                        }
                    }
                    heights[x] = y + 0.5f;
                }

                sw.Stop();
                int toWait = (int) Math.Floor(TARGET_TIME_MS - sw.Elapsed.TotalMilliseconds);
                if (toWait > 0 && run)
                    Thread.Sleep(toWait);
            }

            isRunning = false;
        }
    }

    internal class SkyLightComp : RenderableComponent, IDisposable
    {
        public SkyLight SL;
        public override RectangleF Bounds => Map.Current.Bounds;

        public SkyLightComp(int mapWidth)
        {
            this.SL = new SkyLight(mapWidth);
            base.RenderLayer = Main.LAYER_LIGHT;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            SL?.Render(batcher, camera);
        }

        public void Dispose()
        {
            SL.Dispose();
            SL = null;
        }
    }
}
