using System;
using System.Collections.Generic;
using System.IO;
using Nez;
using Nez.Tools.Atlases;

namespace MTD
{
    public static class RuntimePacker
    {
        private static SpriteAtlasPacker.Config MakeConfig(string src, string dst)
        {
            return new SpriteAtlasPacker.Config()
            {
                InputPaths = new[] { src },
                AtlasOutputFile = dst + ".png",
                MapOutputFile = dst + ".atlas",
                AtlasMaxWidth = 4096,
                AtlasMaxHeight = 4096,
                Padding = 1,
                DontCreateAnimations = true,
                FrameRate = 8,
                IsSquare = false,
                IsPowerOfTwo = true,
                OriginX = 0.5f,
                OriginY = 0.5f,
                OutputLua = false,
                RecurseSubdirectories = true
            };
        }

        private static void EnsureOldIsNotPacked(string src, string dst, Action<string> step)
        {
            string fullSrc = Path.GetFullPath(src).Trim();
            string fullDst = Path.GetFullPath(src).Trim();
            if (fullSrc.StartsWith(fullDst))
            {
                string dstImgPath = dst + ".png";
                if (File.Exists(dstImgPath))
                {
                    step?.Invoke("Deleting old output file...");
                    File.Delete(dstImgPath);
                }
            }
        }

        public static bool PackAll(string source, string dest, Action<string> step = null)
        {
            if (!Directory.Exists(source))
                return false;

            List<string> subs = new List<string>();
            foreach (var dir in Directory.EnumerateDirectories(source, "", SearchOption.AllDirectories))
            {
                bool isSub = Path.GetFileName(dir).StartsWith("[Atlas]");
                if (isSub)
                    subs.Add(dir);
            }

            bool main = Pack(source, dest, step);
            if (!main)
                return false;

            foreach (var sub in subs)
            {
                string folderName = Path.GetFileName(sub).Substring("[Atlas]".Length);
                string subDest = Path.Combine(new DirectoryInfo(sub).Parent.FullName, folderName);

                bool worked = Pack(sub, subDest, step);
                if (!worked)
                    return false;
            }

            return true;
        }

        public static bool Pack(string relativeSource, string relativeDest, Action<string> step = null)
        {
            bool IsFontTexture(string file)
            {
                // Font textures are in the format NAME_X.png where NAME is the name of the font (NAME.fnt)
                // and X is the texture index, an integer.
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.IndexOf('_') == -1)
                    return false;

                string[] split = fileName.Split('_');
                if (split.Length != 2) return false;

                bool secondPartIsNum = int.TryParse(split[1], out int _);
                if (!secondPartIsNum) return false;

                string fontName = split[0];

                var fi = new FileInfo(file);
                string fontPath = Path.Combine(fi.DirectoryName, fontName + ".fnt");
                if (File.Exists(fontPath))
                {
                    Debug.Log($"Excluding font texture: {fileName}.png");
                    return true;
                }
                return false;
            }

            bool IsAtlasTexture(string file)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                // Check that it isn't a sub-atlas.
                string atlasFilePath = Path.Combine(new FileInfo(file).DirectoryName, fileName + ".atlas");
                if (File.Exists(atlasFilePath))
                {
                    Debug.Log($"Excluding sub-atlas texture: {fileName}.atlas");
                    return true;
                }

                return false;
            }

            bool IsNezFolder(string file)
            {
                return Path.GetFullPath(file).Replace("\\", "/").Contains("/nez/");
            }

            bool ShouldPack(string file)
            {
                return !IsNezFolder(file) && !IsFontTexture(file) && !IsAtlasTexture(file);
            }

            try
            {
                EnsureOldIsNotPacked(relativeSource, relativeDest, step);

                var config = MakeConfig(relativeSource, relativeDest);
                int code = SpriteAtlasPacker.PackSprites(config, step, ShouldPack);
                if (code != 0)
                    throw new Exception($"Packing failed with error code {code}: {(SpriteAtlasPacker.FailCode) code}");

                return true;
            }
            catch (Exception e)
            {
                Debug.Error($"Exception packing sprites:\n{e}");
                return false;
            }
        }
    }
}
