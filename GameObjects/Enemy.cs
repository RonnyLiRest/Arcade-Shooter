using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    /// <summary>
    /// Базовый класс для всех врагов. Конкретное поведение движения
    /// и стрельбы описывается в наследниках (BasicEnemy, FastEnemy, ShooterEnemy).
    /// </summary>
    public abstract class Enemy : Entity
    {
        public int ScoreValue { get; protected set; }

        protected Enemy(float x, float y, int width, int height, int maxHealth, float speed, int scoreValue)
            : base(x, y, width, height, maxHealth, speed)
        {
            ScoreValue = scoreValue;
        }

        /// <summary>Правило движения конкретного типа врага.</summary>
        public override void Update(float deltaTime)
        {
            Move(deltaTime);
        }

        protected abstract void Move(float deltaTime);

        /// <summary>Попытка выстрелить по игроку. По умолчанию враг не стреляет.</summary>
        public virtual Bullet? TryShoot(float playerX, float playerY) => null;
    }
}
