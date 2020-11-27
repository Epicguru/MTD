using Microsoft.Xna.Framework;
using Nez.Textures;

namespace Spriter2Nez
{
    /// <summary>
    /// Class for holding the draw info for a sprite.
    /// </summary>
    public class SpriteDrawInfo
    {
        public Sprite Drawable;
        public Vector2 Pivot;
        public Vector2 Position;
        public Vector2 Origin;
        public Vector2 Scale;
        public float Rotation;
        public Color Color;
        public float Depth;
    }
}
