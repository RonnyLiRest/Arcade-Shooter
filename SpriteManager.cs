using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ArcadeShooter.Managers
{
    /// <summary>
    /// Загрузка и кэширование картинок из папки Assets/Images рядом с .exe.
    /// </summary>
    public static class SpriteManager
    {
        /// <summary>Полный список файлов, без которых игра не стартует.</summary>
        public static readonly string[] RequiredFiles =
        {
            "player.png",
            "basic.png", "fast.png", "shooter.png",
            "health.png", "rapidfire.png", "extralife.png",
            "level1_bg.png", "level2_bg.png", "level3_bg.png"
        };

        private static readonly string ImagesFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");

        private static readonly Dictionary<string, Image> Cache = new();

        /// <summary>Возвращает картинку по имени файла (с кэшированием).</summary>
        public static Image Get(string fileName)
        {
            if (Cache.TryGetValue(fileName, out var cached)) return cached;

            // Читаем байты и грузим из памяти, чтобы файл не оставался заблокированным на диске.
            byte[] bytes = File.ReadAllBytes(Path.Combine(ImagesFolder, fileName));
            using var stream = new MemoryStream(bytes);
            var image = Image.FromStream(stream);

            Cache[fileName] = image;
            return image;
        }
    }
}
