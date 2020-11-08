using JDef;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Textures;
using System;

namespace MTD.World
{
    public class TileDef : Def
    {
        /// <summary>
        /// The name of the tile.
        /// </summary>
        public string Name;

        /// <summary>
        /// The class of the tile.
        /// </summary>
        public Type Class;

        /// <summary>
        /// The default sprite of the tile.
        /// </summary>
        public Sprite Sprite;

        /// <summary>
        /// The default color of the tile.
        /// Can be changed at runtime.
        /// </summary>
        public Color Color;

        /// <summary>
        /// The tick interval of the tile.
        /// Tiles are ticked rather than updated.
        /// This means that there will be at most 60 ticks per second, so
        /// when TickInterval is 0 then it will be ticked every time (60 per second).
        /// With TickInterval of 1 it will be ticked every-other time, so 30 times per second.
        /// With TickInterval of 3 it will be ticked once every 3 times, so 20 times per second.
        /// etc.
        /// TickInterval of less than zero will cause the tile to never be ticked.
        /// </summary>
        public int TickInterval;

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrEmpty(Name))
            {
                ValidateError($"Name is null or empty. {DefName} will be used.");
                Name = DefName;
            }

            if (Class == null)
            {
                ValidateError($"Tile class is null. Defaulting to {typeof(Tile).FullName}");
                Class = typeof(Tile);
            }
            else if (!typeof(Tile).IsAssignableFrom(Class))
            {
                ValidateError($"Class {Class.FullName} does not inherit from Tile. Will be changed to Tile.");
                Class = typeof(Tile);
            }

            if (Sprite == null)
            {
                ValidateError("Sprite is null.");
            }
        }

        public virtual Tile CreateTile(int x, int y)
        {
            if (Class == null)
                return null;

            try
            {
                var instance = Activator.CreateInstance(Class, this, x, y);
                return instance as Tile;
            }
            catch (Exception e)
            {
                Debug.Error("Exception creating tile instance for def {0}: {1}'s constructor should have 3 parameters: TileDef def, int x, int y", this, Class.Name);
                Debug.Error(e.ToString());
                return null;
            }
        }
    }
}
