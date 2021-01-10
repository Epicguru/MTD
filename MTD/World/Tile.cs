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

        private static readonly Vector2 origin = new Vector2(SIZE * 0.5f, SIZE * 0.5f);
        private static Vector4[] masks;
        private static readonly Color[] uvColors = new Color[4];
        private static readonly Color[] fullMaskColors =
        {
            Color.Create(0, 0, 0, 0),
            Color.Create(255, 0, 0, 0),
            Color.Create(0, 255, 0, 0),
            Color.Create(255, 255, 0, 0),
        };
        private static readonly Sprite[] overlays = new Sprite[35]; // 1 null, 4 slopes, 15 rectangular, 15 corners
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
                string name2 = $"OverlayCorner_{Convert.ToString(j, 2).PadLeft(4, '0')}";
                overlays[4 + j] = readSprite(name);
                overlays[19 + j] = readSprite(name2);
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
                return IsSolid || Def.CanClimb;
            }
        }
        public virtual bool CanStandIn
        {
            get
            {
                return !IsSolid;
            }
        }
        public virtual bool BlocksLight
        {
            get
            {
                return IsSolid || SlopeIndex != 0;
            }
        }
        public virtual bool IsSolid
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
        public int Hitpoints { get; protected set; }

        public int MaxHitpoints
        {
            get
            {
                return Def.Hitpoints;
            }
        }

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

        private byte overlay1Index, overlay2Index;

        public Tile(TileDef def, int x, int y)
        {
            this.Def = def ?? throw new ArgumentNullException(nameof(def));
            this.X = x;
            this.Y = y;
            this.Hitpoints = MaxHitpoints;
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

            batcher.Draw4Col(spr, new Vector2(X * SIZE, Y * SIZE), uvColors[0], uvColors[1], uvColors[2], uvColors[3], 0f, origin, 1f, SpriteEffects.None, Layer.TileDarkness);

            if (Layer.Depth != 0)
                return;

            if (SlopeIndex != 0)
            {
                batcher.Draw4Col(overlays[SlopeIndex], new Vector2(X * SIZE, Y * SIZE), fullMaskColors[0], fullMaskColors[1], fullMaskColors[2], fullMaskColors[3], 0f, origin, 1f, SpriteEffects.None, 1f);
            }
            else if (!Def.CanClimb)
            {
                if (overlay1Index > 0)
                    batcher.Draw4Col(overlays[overlay1Index + 4], new Vector2(X * SIZE, Y * SIZE), fullMaskColors[0], fullMaskColors[1], fullMaskColors[2], fullMaskColors[3], 0f, origin, 1f, SpriteEffects.None, 1f);

                if (overlay2Index > 0)
                    batcher.Draw4Col(overlays[overlay2Index + 19], new Vector2(X * SIZE, Y * SIZE), fullMaskColors[0], fullMaskColors[1], fullMaskColors[2], fullMaskColors[3], 0f, origin, 1f, SpriteEffects.None, 1f);
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

        private void UpdateOverlayIndices()
        {
            if (Layer.Depth != 0)
                return;

            if (IsSolid && SlopeIndex == 0)
            {
                TileLayer l = Layer;
                int left = IsBoundaryWith(l.GetTile(X - 1, Y)) ? 8 : 0;
                int up = IsBoundaryWith(l.GetTile(X, Y - 1)) ? 4 : 0;
                int right = IsBoundaryWith(l.GetTile(X + 1, Y)) ? 2 : 0;
                int down = IsBoundaryWith(l.GetTile(X, Y + 1)) ? 1 : 0;
                overlay1Index = (byte)(left + up + right + down);

                int tl = left == 0 && up == 0 && IsBoundaryWith(l.GetTile(X - 1, Y - 1)) ? 8 : 0;
                int tr = right == 0 && up == 0 && IsBoundaryWith(l.GetTile(X + 1, Y - 1)) ? 4 : 0;
                int br = right == 0 && down == 0 && IsBoundaryWith(l.GetTile(X + 1, Y + 1)) ? 2 : 0;
                int bl = left == 0 && down == 0 && IsBoundaryWith(l.GetTile(X - 1, Y + 1)) ? 1 : 0;
                overlay2Index = (byte)(tl + tr + br + bl);
            }
            else
            {
                overlay1Index = 0;
                overlay2Index = 0;
            }
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
            if (Hitpoints > MaxHitpoints)
                Hitpoints = MaxHitpoints;
        }

        /// <summary>
        /// Use to the set the hitpoints of this tile.
        /// Setting to zero or less will destroy the tile.
        /// </summary>
        public virtual void SetHitpoints(int hitpoints)
        {
            this.Hitpoints = Math.Clamp(hitpoints, 0, MaxHitpoints);
            if (hitpoints <= 0)
            {
                // Die.
                Layer.Map.SetTile(X, Y, Layer.Depth, null);
            }
        }

        /// <summary>
        /// Called whenever the tile is removed from the tile layer.
        /// Note that this is also called when the world unloads: check the <paramref name="fromUnload"/> parameter.
        /// </summary>
        /// <param name="fromUnload">True if the world is unloading.</param>
        public virtual void OnRemoved(bool fromUnload)
        {

        }

        /// <summary>
        /// Called whenever an adjacent tile, or this tile itself, changes in a way that could affect this one.
        /// Also called when the tile is first placed, after <see cref="OnPlaced(bool)"/>.
        /// For example, if an adjacent tile is removed, this method will be called with <paramref name="changedTile"/> being null.
        /// This will only be called when the adjacent tile is in the same layer as this one.
        /// The default implementation updates this tile's collider (if in layer 0) and recalculates graphical changes.
        /// When overriding this method, be sure to call the base method!
        /// </summary>
        /// <param name="changeX">The X position of changed tile.</param>
        /// <param name="changeY">The Y position of the changed tile.</param>
        /// <param name="changedTile">The changed tile. May be null (when the tile was removed) or may be this tile itself, if this tile has changed.</param>
        public virtual void OnTileChange(int changeX, int changeY, Tile changedTile)
        {
#if DEBUG
            Debug.DrawHollowBox(new Vector2((X) * SIZE, (Y) * SIZE), SIZE - 20, Color.Yellow, 0.5f);
#endif

            // Always update overlays.
            UpdateOverlayIndices();

            // Check if it is self change...
            if (changeX == X && changeY == Y)
            {
                // If self changed, and self is solid, update collider.
                if (IsSolid)
                {
                    UpdateCollider();
                }
                return;
            }

            // It is an adjacent change...

            // If an adjacent tile was removed, then this tile for sure needs a collider (if self is solid).
            if (changedTile == null && IsSolid)
            {
                CreateCollider();
            }
            else if (changedTile != null)
            {
                // An adjacent tile was changed or placed.
                // This may require for the collider to be either removed or created.
                UpdateCollider();
            }
        }

        /// <summary>
        /// Removes, places or updates this tile's collider as required.
        /// </summary>
        public virtual void UpdateCollider()
        {
            // Only layer 0 can have colliders.
            if (Layer.Depth != 0)
                return;
            // Non-solid tiles don't have colliders, so nothing needs updating.
            if (!IsSolid)
                return;

            // If any of the surrounding tiles are air or otherwise not solid, this tile needs a collider.
            bool hasAir = AreAnySurroundingsNotSolid();

            if (hasAir)
                CreateCollider(); // Adjacent to non-solid, create collider.
            else
                RemoveCollider(); // All surrounding tiles are solid, no need for a collider.
        }

        private bool AreAnySurroundingsNotSolid()
        {
            var t = Layer.GetTile(X - 1, Y);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X - 1, Y - 1);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X, Y - 1);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X + 1, Y - 1);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X + 1, Y);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X + 1, Y + 1);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X, Y + 1);
            if (t == null || !t.IsSolid)
                return true;
            t = Layer.GetTile(X - 1, Y + 1);
            if (t == null || !t.IsSolid)
                return true;
            return false;
        }

        /// <item>0: No slope (full tile)</item>
        /// <item>1: Left-above slope</item>
        /// <item>2: Right-above slope</item>
        /// <item>3: Right-below slope</item>
        /// <item>4: Left-below slope</item>
        private static readonly Vector2[] Slope1Points = { new Vector2(SIZE * -0.5f, SIZE * -0.5f), new Vector2(SIZE * 0.5f, SIZE * -0.5f), new Vector2(SIZE * -0.5f, SIZE * 0.5f) };
        private static readonly Vector2[] Slope2Points = { new Vector2(SIZE * -0.5f, SIZE * -0.5f), new Vector2(SIZE * 0.5f, SIZE * -0.5f), new Vector2(SIZE * 0.5f, SIZE * 0.5f) };
        private static readonly Vector2[] Slope3Points = { new Vector2(SIZE * 0.5f, SIZE * -0.5f), new Vector2(SIZE * 0.5f, SIZE * 0.5f), new Vector2(SIZE * -0.5f, SIZE * 0.5f) };
        private static readonly Vector2[] Slope4Points = { new Vector2(SIZE * -0.5f, SIZE * -0.5f), new Vector2(SIZE * 0.5f, SIZE * 0.5f), new Vector2(SIZE * -0.5f, SIZE * 0.5f) };
        protected virtual void CreateCollider()
        {
            if (Layer.Depth != 0)
                return;
#if DEBUG
            if (!IsSolid)
                throw new Exception($"CreateCollider() called on non-solid tile.");
#endif
            Collider c;
            switch (SlopeIndex)
            {
                case 0:
                    c = new BoxCollider(SIZE, SIZE);
                    break;
                case 1:
                    c = new PolygonCollider(Slope1Points, false);
                    break;
                case 2:
                    c = new PolygonCollider(Slope2Points, false);
                    break;
                case 3:
                    c = new PolygonCollider(Slope3Points, false);
                    break;
                case 4:
                    c = new PolygonCollider(Slope4Points, false);
                    break;
                default:
                    c = null;
                    break;
            }

            Layer.Map.SetCollider(c, X, Y);
        }

        protected virtual void RemoveCollider()
        {
            if (Layer.Depth != 0)
                return;
#if DEBUG
            if (!IsSolid)
                throw new Exception($"RemoveCollider() called on non-solid tile.");
#endif
            Layer.Map.SetCollider(null, X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Layer?.Depth ?? -1}) {Name}, {Hitpoints}/{MaxHitpoints}";
        }
    }
}
