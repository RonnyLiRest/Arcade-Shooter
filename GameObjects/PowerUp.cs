using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    public enum PowerUpType
    {
        Health,
        RapidFire,
        ExtraLife
    }

    /// <summary>Бонус, падающий вниз, который игрок может подобрать.</summary>
    public class PowerUp : GameObject
    {
        public PowerUpType Type { get; }
        private const float FallSpeed = 80f;

        public PowerUp(float x, float y, PowerUpType type)
            : base(x, y, 30, 30)
        {
            Type = type;
        }

        public override void Update(float deltaTime)
        {
            Y += FallSpeed * deltaTime;
        }

        /// <summary>Применяет эффект бонуса к игроку.</summary>
        public void ApplyTo(Player player)
        {
            switch (Type)
            {
                case PowerUpType.Health:
                    player.TakeDamage(-25); // отрицательный урон = лечение
                    break;
                case PowerUpType.RapidFire:
                    player.ActivateRapidFire(durationSeconds: 15f);
                    break;
                case PowerUpType.ExtraLife:
                    player.Lives++;
                    break;
            }
        }

        public override void Draw(Graphics g)
        {
            string spriteName = Type switch
            {
                PowerUpType.Health => "health.png",
                PowerUpType.RapidFire => "rapidfire.png",
                _ => "extralife.png"
            };
            DrawSprite(g, spriteName);
        }
    }
}
