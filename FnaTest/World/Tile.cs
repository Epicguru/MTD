using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using System;
using System.Runtime.CompilerServices;
using Nez.Textures;

namespace MTD.World
{
    public class Tile
    {
        public const int SIZE = 64;

        private static Vector4[] masks;
        private static readonly Color[] uvColors = new Color[4];
        private static readonly Color[] fullMaskColors = new Color[4]
        {
            Color.Create(0, 0, 0, 0),
            Color.Create(255, 0, 0, 0),
            Color.Create(0, 255, 0, 0),
            Color.Create(255, 255, 0, 0),
        };
        private static readonly Sprite[] overlays = new Sprite[20]; // 1 null, 4 slopes, 15 rectangular
        private static byte lastMask;
        internal static void LoadMasks(SpriteAtlas atlas)
        {
            Sprite readSprite(string name)
            {
                return atlas.GetSprite($"Tiles/{name}");
            }
            Vector4 read(string name)
            {
                var rect = readSprite(name).Uvs;
                return new Vector4(rect.X, rect.Y, rect.Width, rect.Height);
            }

            masks = new Vector4[5];
            int i = 0;
            masks[i++] = read("FullMask");

            masks[i++] = read("Slope_LeftUp");
            masks[i++] = read("Slope_RightUp");
            masks[i++] = read("Slope_RightDown");
            masks[i++] = read("Slope_LeftDown");

            i = 1;
            overlays[i++] = readSprite("Overlay_LeftUp");
            overlays[i++] = readSprite("Overlay_RightUp");
            overlays[i++] = readSprite("Overlay_RightDown");
            overlays[i++] = readSprite("Overlay_LeftDown");

            for (int j = 1; j <= 15; j++)
            {
                string name = $"Overlay_{Convert.ToString(j, 2).PadLeft(4, '0')}";
                overlays[4 + j] = readSprite(name);
            }
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

        /// <summary>
        /// The current slope state of this tile.
        /// <list type="table">
        /// <item>0: No slope (full tile)</item>
        /// <item>1: Left-above slope</item>
        /// <item>2: Right-above slope</item>
        /// <item>3: Right-below slope</item>
        /// <item>4: Left-below slope</item>
        /// </list>
        /// </summary>
        public byte SlopeIndex;

        public Tile(TileDef def, int x, int y)
        {
            this.Def = def ?? throw new ArgumentNullException(nameof(def));
            this.X = x;
            this.Y = y;
        }

        public virtual bool AutoSlopeWith(Tile other)
        {
            if (other == null)
                return false;

            return !other.CanStandIn;
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
        /// 
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Draw(Batcher batcher, Camera c)
        {
            var spr = Def.Sprite;
            if (spr == null)
                return;

            byte mask = SlopeIndex;
            if (lastMask != mask)
                MakeColors(mask);

            batcher.Draw4Col(spr, new Vector2(X * SIZE, Y * SIZE), uvColors[0], uvColors[1], uvColors[2], uvColors[3], 0f, spr.Origin, 1f, SpriteEffects.None, Layer.TileDarkness);

            if (Layer.Depth != 0)
                return;

            if (SlopeIndex != 0)
            {
                batcher.Draw4Col(overlays[SlopeIndex], new Vector2(X * SIZE, Y * SIZE), fullMaskColors[0], fullMaskColors[1], fullMaskColors[2], fullMaskColors[3], 0f, overlays[SlopeIndex].Origin, 1f, SpriteEffects.None, 1f);
            }
            else
            {
                TileLayer l = Layer;
                int left = IsBoundaryWith(l.GetTile(X - 1, Y)) ? 8 : 0;
                int up = IsBoundaryWith(l.GetTile(X, Y - 1)) ? 4 : 0;
                int right = IsBoundaryWith(l.GetTile(X + 1, Y)) ? 2 : 0;
                int down = IsBoundaryWith(l.GetTile(X, Y + 1)) ? 1 : 0;
                int index = left + up + right + down;
                if(index > 0)
                    batcher.Draw4Col(overlays[index + 4], new Vector2(X * SIZE, Y * SIZE), fullMaskColors[0], fullMaskColors[1], fullMaskColors[2], fullMaskColors[3], 0f, overlays[index + 4].Origin, 1f, SpriteEffects.None, 1f);
            }

#if DEBUG
            if (Main.DebugStandableTiles)
            {
                bool canStand = Layer.Map.CanStandAt(X, Y - 1);
                if (canStand)
                {
                    Debug.DrawHollowRect(new RectangleF((X - 0.5f) * SIZE + 4, (Y - 1.5f) * SIZE + 4, SIZE - 8, SIZE - 8), Color.Green);
                }
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool IsBoundaryWith(Tile tile) => tile == null || tile.Def != this.Def;

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
