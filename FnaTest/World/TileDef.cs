using JDef;
using Nez.Textures;
using System;

namespace MTD.World
{
    public class TileDef : Def
    {
        public string Name;
        public Type Class;
        public Sprite Sprite;

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
    }
}
