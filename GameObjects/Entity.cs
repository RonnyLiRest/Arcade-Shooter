namespace ArcadeShooter.GameObjects
{
    /// <summary>
    /// Промежуточный абстрактный класс для "живых" объектов —
    /// тех, у кого есть здоровье и скорость перемещения (игрок, враги).
    /// </summary>
    public abstract class Entity : GameObject
    {
        public int MaxHealth { get; protected set; }
        public int Health { get; protected set; }
        public float Speed { get; protected set; }

        public bool IsAlive => Health > 0;

        protected Entity(float x, float y, int width, int height, int maxHealth, float speed)
            : base(x, y, width, height)
        {
            MaxHealth = maxHealth;
            Health = maxHealth;
            Speed = speed;
        }

        /// <summary>Наносит урон; отрицательное значение работает как лечение.</summary>
        public virtual void TakeDamage(int amount)
        {
            Health -= amount;
            if (Health > MaxHealth) Health = MaxHealth;
            if (Health <= 0)
            {
                Health = 0;
                IsActive = false;
            }
        }
    }
}
