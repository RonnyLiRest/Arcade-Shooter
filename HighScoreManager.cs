using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArcadeShooter.Managers
{
    /// <summary>
    /// Сохраняет и загружает таблицу рекордов в текстовый файл
    /// в папке %AppData%\ArcadeShooter\highscores.txt.
    /// </summary>
    public static class HighScoreManager
    {
        private static readonly string FolderPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArcadeShooter");

        private static readonly string FilePath = Path.Combine(FolderPath, "highscores.txt");

        public static List<int> Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new List<int>();

                return File.ReadAllLines(FilePath)
                    .Select(line => int.TryParse(line, out var value) ? value : 0)
                    .OrderByDescending(v => v)
                    .Take(10)
                    .ToList();
            }
            catch (IOException)
            {
                // Файл недоступен (занят/повреждён) — работаем без сохранённых рекордов.
                return new List<int>();
            }
        }

        public static void SaveScore(int newScore)
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                var scores = Load();
                scores.Add(newScore);
                var top10 = scores.OrderByDescending(v => v).Take(10);
                File.WriteAllLines(FilePath, top10.Select(v => v.ToString()));
            }
            catch (IOException)
            {
                // Сохранение рекорда не критично для работы игры — молча пропускаем.
            }
        }
    }
}
