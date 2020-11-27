using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Systems;
using Nez.Textures;
using SpriterDotNet;
using SpriterDotNet.Providers;
using System;
using System.Collections.Generic;
using System.IO;

namespace Spriter2Nez
{
    public class SpriterContentLoader
    {
        public Spriter Spriter { get; private set; }
        public bool UsingSpriteAtlases
        {
            get
            {
                return atlases != null && atlases.Count > 0;
            }
        }

        private readonly NezContentManager content;
        private readonly string scmlPath;
        private readonly string rootPath;

        private List<SpriteAtlas> atlases;

        public SpriterContentLoader(NezContentManager content, string scmlPath)
        {
            this.content = content;
            this.scmlPath = scmlPath;
            rootPath = scmlPath.Substring(0, scmlPath.LastIndexOf("/", StringComparison.Ordinal));
        }

        public void AddAtlas(SpriteAtlas atlas, bool atStart = false)
        {
            if (atlas == null)
                return;

            atlases ??= new List<SpriteAtlas>();

            if (atStart)
                atlases.Insert(0, atlas);
            else
                atlases.Add(atlas);
        }

        public void Fill(DefaultProviderFactory<Sprite, SoundEffect> factory)
        {
            if (Spriter == null)
                Load();

            foreach (SpriterFolder folder in Spriter.Folders)
            {
                AddRegularFolder(folder, factory);
            }
        }

        /// <summary>
        /// Gets one of the loaded <see cref="SpriterEntity"/> by name.
        /// Will return null if not loaded or not found.
        /// Name is case sensitive and whitespace matters.
        /// </summary>
        public SpriterEntity GetEntity(string name)
        {
            if (Spriter == null)
            {
                Debug.Error($"Failed to find spriter entity '{name}', because Fill() has not been called.");
                return null;
            }

            if (name == null)
                return null;
            if (Spriter.Entities == null)
                return null;

            foreach (var e in Spriter.Entities)
            {
                if (e.Name == name)
                    return e;
            }
            return null;
        }

        private void AddRegularFolder(SpriterFolder folder, DefaultProviderFactory<Sprite, SoundEffect> factory)
        {
            foreach (SpriterFile file in folder.Files)
            {
                string path = $"{rootPath}/{file.Name}";

                if (file.Type == SpriterFileType.Sound)
                {
                    SoundEffect sound = content.Load<SoundEffect>(path);
                    factory.SetSound(Spriter, folder, file, sound);
                }
                else
                {
                    Sprite sprite = null;
                    if (UsingSpriteAtlases)
                    {
                        string atlasPath = path.Substring(0, path.Length - 4); // Remove the .png
                        foreach (var atlas in atlases)
                        {
                            var spr = atlas.GetSprite(atlasPath);
                            if (spr != null)
                            {
                                sprite = spr;
                                break;
                            }
                        }
                        if(sprite == null)
                            Debug.Error($"Failed to find sprite '{atlasPath}' in any atlas! Falling back to texture load.");
                    }
                    if(sprite == null)
                    {
                        // No atlases are being used, or failed to find sprite in atlas.
                        Texture2D texture = content.Load<Texture2D>(path);
                        if (texture == null)
                        {
                            Debug.Error($"Failed to find texture '{path}'");
                        }
                        sprite = new Sprite(texture);
                    }
                    
                    factory.SetSprite(Spriter, folder, file, sprite);
                }

            }
        }

        private void Load()
        {
            string projectPath = content.RootDirectory + "/" + scmlPath + ".scml";
            string data = File.ReadAllText(projectPath);

            Spriter = SpriterReader.Default.Read(data);
        }
    }
}
