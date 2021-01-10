using JDef;
using Nez;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MTD.World
{
    public class TileDef : Def
    {
        #region Static Methods

        public static IReadOnlyList<TileDef> All { get { return allDefs;} }
        private static readonly Dictionary<string, TileDef> namedDefs = new Dictionary<string, TileDef>();
        private static TileDef[] allDefs;

        internal static void Load()
        {
            var db = Main.Defs;
            var all = db.GetAllOfType<TileDef>();
            foreach (var def in all)
            {
                namedDefs.Add(def.DefName, def);
            }
            allDefs = namedDefs.Values.ToArray();
        }

        public static TileDef Get(string defName)
        {
            if (namedDefs.TryGetValue(defName, out var def))
                return def;
            return null;
        }

        #endregion

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

        /// <summary>
        /// Can pawns climb this tile?
        /// </summary>
        public bool CanClimb;

        /// <summary>
        /// The max hitpoints for this tile.
        /// </summary>
        public int Hitpoints;

        public override void Validate()
        {
            base.Validate();

            if (!DefName.EndsWith("Tile"))
                ValidateWarn($"TileDef's should have a name that ends with 'Tile'. Suggested name: '{DefName}Tile'");

            if (Sprite == null)
            {
                ValidateError("This TileDef is missing it's Sprite.");
                // TODO replace with placeholder sprite.
            }

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

            if (Hitpoints <= 0)
            {
                ValidateError("Hitpoints cannot be zero or less! Setting to 1hp.");
                Hitpoints = 1;
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
                Debug.Error($"Exception creating tile instance for def {this}: {Class.Name}'s constructor should have 3 parameters: TileDef def, int x, int y");
                Debug.Error(e.ToString());
                return null;
            }
        }
    }
}
