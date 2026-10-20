using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ArcadeShooter.GameObjects;

namespace ArcadeShooter.Managers
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver,
        HighScores   // таблица рекордов, открытая из главного меню (не после партии)
    }

    /// <summary>Описание одного из трёх уровней сложности.</summary>
    public readonly struct LevelConfig
    {
        public int Number { get; }
        public int ScoreToReach { get; }
        public float SpawnInterval { get; }
        public int BasicWeight { get; }
        public int FastWeight { get; }
        public int ShooterWeight { get; }
        public Color BackgroundColor { get; }
        public string Name { get; }

        public LevelConfig(int number, int scoreToReach, float spawnInterval, int basicWeight, int fastWeight, int shooterWeight, Color backgroundColor, string name)
        {
            Number = number;
            ScoreToReach = scoreToReach;
            SpawnInterval = spawnInterval;
            BasicWeight = basicWeight;
            FastWeight = fastWeight;
            ShooterWeight = shooterWeight;
            BackgroundColor = backgroundColor;
            Name = name;
        }
    }

    /// <summary>
    /// Центральный класс игровой логики: хранит игрока, врагов, пули и
    /// бонусы, отвечает за их создание, обновление и обработку столкновений.
    /// Форма (MainForm) только вызывает Update()/Draw() и не знает деталей правил игры.
    /// </summary>
    public class GameManager
    {
        private static readonly LevelConfig[] Levels =
        {
            new(1, scoreToReach: 0,   spawnInterval: 1.40f, basicWeight: 10, fastWeight: 0, shooterWeight: 0, backgroundColor: Color.FromArgb(10, 15, 35),  name: "Разведка"),
            new(2, scoreToReach: 150, spawnInterval: 1.00f, basicWeight: 6,  fastWeight: 4, shooterWeight: 1, backgroundColor: Color.FromArgb(30, 10, 35),  name: "Натиск"),
            new(3, scoreToReach: 400, spawnInterval: 0.65f, basicWeight: 3,  fastWeight: 4, shooterWeight: 3, backgroundColor: Color.FromArgb(35, 8, 8),    name: "Хаос"),
        };

        // Минимальное время между появлениями RapidFire-бонуса после его активации.
        private const float RapidFireMinGap = 15f;

        // Веса типов бонусов при спавне: жизнь выпадает заметно реже остальных.
        private const int HealthWeight = 8;
        private const int RapidFireWeight = 3;
        private const int ExtraLifeWeight = 1;

        // Бонусы появляются по отдельному таймеру со случайной паузой между ними —
        // так они не кучкуются, как при спавне вместе с врагами.
        private const float PowerUpMinInterval = 8f;
        private const float PowerUpMaxInterval = 16f;

        public Rectangle FieldBounds { get; }
        public Player Player { get; private set; }
        public List<Enemy> Enemies { get; } = new();
        public List<Bullet> Bullets { get; } = new();
        public List<PowerUp> PowerUps { get; } = new();

        public GameState State { get; private set; } = GameState.Menu;
        public LevelConfig CurrentLevel { get; private set; } = Levels[0];

        /// <summary>Топ-10 рекордов — актуализируется и после партии, и при открытии из меню.</summary>
        public List<int> LastHighScores { get; private set; } = new();

        public event Action<int>? ScoreChanged;
        public event Action? GameOver;
        public event Action? PlayerDamaged; // форма подписывается на него для эффекта тряски экрана

        private readonly Random _random = new();
        private float _spawnTimer;
        private float _powerUpTimer;          // обратный отсчёт до следующего бонуса
        private float _rapidFireSpawnCooldown; // пока > 0 — RapidFire не может выпасть повторно

        public GameManager(Rectangle fieldBounds)
        {
            FieldBounds = fieldBounds;
            Player = new Player(0, 0);
            Player.Respawn(FieldBounds);
            LastHighScores = HighScoreManager.Load();
        }

        public void StartNewGame()
        {
            Enemies.Clear();
            Bullets.Clear();
            PowerUps.Clear();
            Player = new Player(0, 0);
            Player.Respawn(FieldBounds);
            CurrentLevel = Levels[0];
            _spawnTimer = 0f;
            _rapidFireSpawnCooldown = 0f;
            _powerUpTimer = NextPowerUpDelay();
            State = GameState.Playing;
            ScoreChanged?.Invoke(Player.Score);
        }

        public void HandleShootInput()
        {
            if (State != GameState.Playing) return;

            Bullets.AddRange(Player.TryShoot());
        }

        public void PauseGame()
        {
            if (State == GameState.Playing) State = GameState.Paused;
        }

        public void ResumeGame()
        {
            if (State == GameState.Paused) State = GameState.Playing;
        }

        /// <summary>Открывает таблицу рекордов из главного меню (без запуска партии).</summary>
        public void ShowHighScoresScreen()
        {
            LastHighScores = HighScoreManager.Load();
            State = GameState.HighScores;
        }

        /// <summary>Выход в главное меню из паузы, таблицы рекордов или экрана Game Over.
        /// Текущий прогресс (если партия ещё идёт) не сохраняется.</summary>
        public void ReturnToMenu()
        {
            Enemies.Clear();
            Bullets.Clear();
            PowerUps.Clear();
            State = GameState.Menu;
        }

        public void Update(float deltaTime)
        {
            if (State != GameState.Playing) return;

            Player.Update(deltaTime);
            if (_rapidFireSpawnCooldown > 0f) _rapidFireSpawnCooldown -= deltaTime;

            UpdateSpawning(deltaTime);
            UpdatePowerUpSpawning(deltaTime);
            UpdateLevel();

            foreach (var enemy in Enemies)
            {
                enemy.Update(deltaTime);

                var enemyShot = enemy.TryShoot(Player.X, Player.Y);
                if (enemyShot != null) Bullets.Add(enemyShot);
            }

            foreach (var bullet in Bullets) bullet.Update(deltaTime);
            foreach (var powerUp in PowerUps) powerUp.Update(deltaTime);

            HandleCollisions();
            RemoveOffscreenAndDeadObjects();

            if (Player.Lives <= 0 && Player.IsAlive == false)
            {
                EndGame();
            }
        }

        private void UpdateSpawning(float deltaTime)
        {
            _spawnTimer += deltaTime;
            if (_spawnTimer < CurrentLevel.SpawnInterval) return;

            _spawnTimer = 0f;
            SpawnRandomEnemy();
        }

        private float NextPowerUpDelay() =>
            PowerUpMinInterval + (float)_random.NextDouble() * (PowerUpMaxInterval - PowerUpMinInterval);

        private void UpdatePowerUpSpawning(float deltaTime)
        {
            _powerUpTimer -= deltaTime;
            if (_powerUpTimer > 0f) return;

            SpawnRandomPowerUp();
            _powerUpTimer = NextPowerUpDelay();
        }

        private void SpawnRandomEnemy()
        {
            float x = _random.Next(FieldBounds.Left, FieldBounds.Right - 50);
            float y = FieldBounds.Top - 40;

            int totalWeight = CurrentLevel.BasicWeight + CurrentLevel.FastWeight + CurrentLevel.ShooterWeight;
            int roll = _random.Next(0, totalWeight);

            Enemy enemy;
            if (roll < CurrentLevel.BasicWeight)
            {
                enemy = new BasicEnemy(x, y);
            }
            else if (roll < CurrentLevel.BasicWeight + CurrentLevel.FastWeight)
            {
                enemy = new FastEnemy(x, y);
            }
            else
            {
                enemy = new ShooterEnemy(x, y);
            }

            Enemies.Add(enemy);
        }

        /// <summary>
        /// Взвешенный выбор типа бонуса: жизнь выпадает редко (вес 1),
        /// RapidFire временно исключается из пула, пока не истёк RapidFireMinGap
        /// после его последней активации.
        /// </summary>
        private void SpawnRandomPowerUp()
        {
            var pool = new List<(PowerUpType type, int weight)> { (PowerUpType.Health, HealthWeight) };

            if (_rapidFireSpawnCooldown <= 0f)
            {
                pool.Add((PowerUpType.RapidFire, RapidFireWeight));
            }

            pool.Add((PowerUpType.ExtraLife, ExtraLifeWeight));

            int totalWeight = pool.Sum(p => p.weight);
            int roll = _random.Next(0, totalWeight);

            PowerUpType chosen = pool[0].type;
            int cumulative = 0;
            foreach (var (type, weight) in pool)
            {
                cumulative += weight;
                if (roll < cumulative)
                {
                    chosen = type;
                    break;
                }
            }

            float x = _random.Next(FieldBounds.Left, FieldBounds.Right - 30);
            PowerUps.Add(new PowerUp(x, FieldBounds.Top - 30, chosen));
        }

        private void UpdateLevel()
        {
            for (int i = Levels.Length - 1; i >= 0; i--)
            {
                if (Player.Score >= Levels[i].ScoreToReach)
                {
                    CurrentLevel = Levels[i];
                    return;
                }
            }
        }

        private void HandleCollisions()
        {
            foreach (var bullet in Bullets.Where(b => b.Owner == BulletOwner.Player && b.IsActive))
            {
                foreach (var enemy in Enemies.Where(e => e.IsActive))
                {
                    if (!CollisionManager.Intersects(bullet.Bounds, enemy.Bounds)) continue;

                    enemy.TakeDamage(bullet.Damage);
                    bullet.IsActive = false;

                    if (!enemy.IsAlive)
                    {
                        Player.Score += enemy.ScoreValue;
                        ScoreChanged?.Invoke(Player.Score);
                    }
                    break;
                }
            }

            foreach (var bullet in Bullets.Where(b => b.Owner == BulletOwner.Enemy && b.IsActive))
            {
                if (!CollisionManager.Intersects(bullet.Bounds, Player.Bounds)) continue;

                bullet.IsActive = false;
                DamagePlayer(bullet.Damage);
            }

            foreach (var enemy in Enemies.Where(e => e.IsActive))
            {
                if (!CollisionManager.Intersects(enemy.Bounds, Player.Bounds)) continue;

                enemy.IsActive = false;
                DamagePlayer(25);
            }

            foreach (var powerUp in PowerUps.Where(p => p.IsActive))
            {
                if (!CollisionManager.Intersects(powerUp.Bounds, Player.Bounds)) continue;

                if (powerUp.Type == PowerUpType.RapidFire)
                {
                    _rapidFireSpawnCooldown = RapidFireMinGap;
                }

                powerUp.ApplyTo(Player);
                powerUp.IsActive = false;
            }
        }

        private void DamagePlayer(int amount)
        {
            Player.TakeDamage(amount);
            PlayerDamaged?.Invoke();
            if (!Player.IsAlive)
            {
                Player.Lives--;
                if (Player.Lives > 0)
                {
                    Player.Respawn(FieldBounds);
                }
            }
        }

        private void RemoveOffscreenAndDeadObjects()
        {
            Bullets.RemoveAll(b => !b.IsActive || b.Y < FieldBounds.Top - 20 || b.Y > FieldBounds.Bottom + 20);
            Enemies.RemoveAll(e => !e.IsActive || e.Y > FieldBounds.Bottom + 40);
            PowerUps.RemoveAll(p => !p.IsActive || p.Y > FieldBounds.Bottom + 20);
        }

        private void EndGame()
        {
            State = GameState.GameOver;
            HighScoreManager.SaveScore(Player.Score);
            LastHighScores = HighScoreManager.Load();
            GameOver?.Invoke();
        }

        public void DrawAll(Graphics g)
        {
            Player.Draw(g);
            foreach (var enemy in Enemies) enemy.Draw(g);
            foreach (var bullet in Bullets) bullet.Draw(g);
            foreach (var powerUp in PowerUps) powerUp.Draw(g);
        }
    }
}
