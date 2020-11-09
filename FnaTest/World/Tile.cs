using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using System;

namespace MTD.World
{
    public class Tile
    {
        public const int SIZE = 16;

        public TileLayer Layer { get; internal set; }
        public string Name
        {
            get
            {
                return Def?.Name;
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

        /// <summary>
        /// Renders the tile to it's current position and state.
        /// </summary>
        public virtual void Draw(Batcher b, Camera c, float depth)
        {
            var spr = Def.Sprite;
            if (spr == null)
                return;
            b.Draw(spr, new Vector2(X * SIZE, Y * SIZE), this.Color, 0f, spr.Origin, 1f, SpriteEffects.None, depth);
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
