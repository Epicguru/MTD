using Nez;
using Nez.Tools.Atlases;
using System;
using System.IO;

namespace FnaTest
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

        public static bool Pack(string relativeSource, string relativeDest, Action<string> step = null)
        {
            Func<string, bool> ShouldPack = (file) =>
            {
                // Font textures are in the format NAME_X.png where NAME is the name of the font (NAME.fnt)
                // and X is the texture index, an integer.
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.IndexOf('_') == -1)
                    return true;

                string[] split = fileName.Split('_');
                if (split.Length != 2)
                    return true;

                bool secondPartIsNum = int.TryParse(split[1], out int _);
                if (!secondPartIsNum)
                    return true;

                string fontName = split[0];

                var fi = new FileInfo(file);
                string fontPath = Path.Combine(fi.DirectoryName, fontName + ".fnt");
                bool add = !File.Exists(fontPath);

                if (!add)
                    Debug.Log("Excluding font texture: {0}{1}", fileName, ".png");

                return add;
            };

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
                Debug.Error("Exception packing sprites:\n{0}", e);
                return false;
            }
            
        }
    }
}
