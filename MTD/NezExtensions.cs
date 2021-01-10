using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Systems;
using Nez.Textures;
using Spriter2Nez;

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
                        Debug.Error($"Failed to find frame {spriteName} (frame num {i}) for animation. Further missing sprites will not be logged.");
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
                Debug.Error($"Failed to find a total of {notFoundCount} frames, out of a total {frameCount}");
            return anim;
        }

        /// <summary>
        /// Gets a random element of this collection.
        /// Will return default(T) if the collection is null or empty.
        /// </summary>
        public static T GetRandom<T>(this IReadOnlyList<T> collection)
        {
            if (collection == null)
                return default;

            if (collection.Count == 0)
                return default;
            if (collection.Count == 1)
                return collection[0];

            int index = Random.Range(0, collection.Count);
            return collection[index];
        }

        private static Dictionary<string, SpriterContentLoader> spriterLoaders = new Dictionary<string, SpriterContentLoader>();

        /// <summary>
        /// Loads a Spriter project, which can be used to create <see cref="NezAnimator"/>s
        /// and <see cref="AnimationRenderer"/>'s. 
        /// </summary>
        /// <param name="content">The content loader to use.</param>
        /// <param name="path">The path, relative to the Content folder, and not including the file extension (.scml)</param>
        /// <param name="additionalAtlases">Any additional atlses to use. By default uses only the Main atlas.</param>
        /// <returns>The Spriter content loader, or null if loading failed.</returns>
        public static SpriterContentLoader LoadAnimationProject(this NezContentManager content, string path, params SpriteAtlas[] additionalAtlases)
        {
            if (path == null)
                return null;

            if (spriterLoaders.TryGetValue(path, out var found))
                return found;

            if (content == null)
                return null;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            SpriterContentLoader loader = new SpriterContentLoader(content, path);
            loader.AddAtlas(Main.Atlas);
            for (int i = additionalAtlases.Length - 1; i >= 0; i--)
            {
                var atlas = additionalAtlases[i];
                loader.AddAtlas(atlas, true);
            }
            loader.Fill(NezAnimator.DefaultProviderFactory);
            sw.Stop();

            spriterLoaders.Add(path, loader);
            Debug.Trace($"Finished loading spriter project {path}, took {sw.ElapsedMilliseconds} ms");

            return loader;
        }
    }
}
