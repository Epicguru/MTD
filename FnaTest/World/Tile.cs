using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using System;
using System.Runtime.CompilerServices;

namespace MTD.World
{
    public class Tile
    {
        public const int SIZE = 64;

        private static Vector4[] masks;
        private static readonly Color[] uvColors = new Color[4];
        private static byte lastMask;
        internal static void LoadMasks(SpriteAtlas atlas)
        {
            Vector4 read(string name)
            {
                var rect = atlas.GetSprite($"Tiles/{name}").Uvs;
                return new Vector4(rect.X, rect.Y, rect.Width, rect.Height);
            }

            masks = new Vector4[3];
            int i = 0;
            masks[i++] = read("FullMask");
            masks[i++] = read("SlopeMask");
            masks[i++] = read("TopCutMask");
        }
        internal static void StartDraw(EffectParameter maskParam, EffectParameter matrixParam)
        {
            maskParam.SetValue(Tile.masks);
            matrixParam.SetValue(Map.Current?.Entity?.Scene?.Camera?.ViewProjectionMatrix ?? Matrix.Identity);
        }

        public TileLayer Layer { get; internal set; }
        public string Name
        {
            get
            {
                return Def?.Name;
            }
        }

        public virtual bool CanStandOn
        {
            get
            {
                return true;
            }
        }

        public virtual bool CanStandIn
        {
            get
            {
                return Def.CanClimb;
            }
        }

        public virtual bool BlocksLight
        {
            get
            {
                return !Def.CanClimb;
            }
        }

        public Collider Collider
        {
            get
            {
                if (Layer?.Depth != 0)
                    return null;

                return Layer?.Map?.GetCollider(X, Y);
            }
        }

        public readonly int X, Y;
        public readonly TileDef Def;

        public Color Color;

        public Tile(TileDef def, int x, int y)
        {
            this.Def = def ?? throw new ArgumentNullException(nameof(def));
            this.X = x;
            this.Y = y;

            Color = Def.Color;
        }

        /// <summary>
        /// Called whenever this tile ticks.
        /// How often it is called depends on the tick rate of this tile.
        /// See <see cref="TileDef.TickInterval"/>.
        /// </summary>
        public virtual void Tick()
        {

        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MakeColors(byte maskIndex)
        {
            uvColors[0] = Color.Create(0, 0, 0, maskIndex);
            uvColors[1] = Color.Create(255, 0, 0, maskIndex);
            uvColors[2] = Color.Create(0, 255, 0, maskIndex);
            uvColors[3] = Color.Create(255, 255, 0, maskIndex);
            lastMask = maskIndex;
        }

        /// <summary>
        /// Renders the tile to it's current position and state.
        /// </summary>
        public virtual void Draw(Batcher batcher, Camera c, float depth)
        {
            var spr = Def.Sprite;
            if (spr == null)
                return;

            byte mask = 0;
            if (lastMask != mask)
                MakeColors(mask);

            batcher.Draw4Col(spr, new Vector2(X * SIZE, Y * SIZE), uvColors[0], uvColors[1], uvColors[2], uvColors[3], 0f, spr.Origin, 1f, SpriteEffects.None, depth);
        }

        /// <summary>
        /// Called whenever the tile is placed into the tile layer.
        /// Note that this is also called when the world is still loading: in this state,
        /// the tiles around this one may not have loaded yet.
        /// Check the <paramref name="fromLoad"/> parameter.
        /// </summary>
        /// <param name="fromLoad">True if the world is still loading. If the world is loading, the entities may not have been spawned and not all tiles will have been loaded yet.</param>
        public virtual void OnPlaced(bool fromLoad)
        {

        }

        /// <summary>
        /// Called whenever the tile is removed from the tile layer.
        /// Note that this is also called when the world unloads: check the <paramref name="fromUnload"/> parameter.
        /// </summary>
        /// <param name="fromUnload">True if the world is unloading.</param>
        public virtual void OnRemoved(bool fromUnload)
        {

        }
    }
}
