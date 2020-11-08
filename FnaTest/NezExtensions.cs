using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;

namespace MTD
{
    public static class NezExtensions
    {
        /// <summary>
        /// Returns true if this entity reference is null or if the entity is destroyed.
        /// </summary>
        public static bool IsNullOrDestroyed(this Entity entity)
        {
            return entity == null || entity.IsDestroyed;
        }

        /// <summary>
        /// Returns true if this component is null, is not part of an entity, or it's entity is destroyed.
        /// </summary>
        public static bool IsNullOrDestroyed(this Component component)
        {
            return component == null || (component.Entity?.IsDestroyed ?? true);
        }

        /// <summary>
        /// Creates a sprite animation from a sequence of frames in a sprite atlas.
        /// Works with a sequence of sprites such as MyImg0, MyImg1, MyImg2 etc.
        /// </summary>
        /// <param name="atlas">The sprite atlas that contains all frames.</param>
        /// <param name="name">The sprite prefix, such as MyImg</param>
        /// <param name="frameRate">The target frame rate, in frames per second.</param>
        /// <param name="frameCount">The total number of frames in the animation</param>
        /// <param name="frameStep">The index frame increment. Default value 1 results in frame names such as Img0, Img1, Img2 etc. A value of 2 would result in Img0, Img2, Img4 etc.</param>
        /// <returns></returns>
        public static SpriteAnimation CreateAnimation(this SpriteAtlas atlas, string name, float frameRate, int frameCount, int frameStep = 1, Vector2? spriteOrigin = null)
        {
            Sprite[] sprites = new Sprite[frameCount];
            var anim = new SpriteAnimation(sprites, frameRate);
            int notFoundCount = 0;
            for (int i = 0; i < frameCount; i++)
            {
                int frameIndex = i * frameStep;
                string spriteName = name + frameIndex;
                var found = atlas.GetSprite(spriteName);
                if (found == null)
                {
                    if(notFoundCount == 0)
                        Debug.Error("Failed to find frame {0} (frame num {1}) for animation. Further missing sprites will not be logged.", spriteName, i);
                    notFoundCount++;
                }
                else
                {
                    if (spriteOrigin != null)
                        found.OriginNormalized = spriteOrigin.Value;
                    sprites[i] = found;
                }
            }
            if(notFoundCount > 0)
                Debug.Error("Failed to find a total of {0} frames, out of a total {1}", notFoundCount, frameCount);
            return anim;
        }
    }
}
