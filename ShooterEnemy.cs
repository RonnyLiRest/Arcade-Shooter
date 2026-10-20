using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    /// <summary>Медленный, но живучий враг, периодически стреляющий вниз по игроку.</summary>
    public class ShooterEnemy : Enemy
    {
        private const float FireInterval = 1.6f;
        private float _fireTimer;

        public ShooterEnemy(float x, float y)
            : base(x, y, width: 50, height: 40, maxHealth: 4, speed: 55f, scoreValue: 30)
        {
            _fireTimer = FireInterval;
        }

        protected override void Move(float deltaTime)
        {
            Y += Speed * deltaTime;
            _fireTimer -= deltaTime;
        }

        public override Bullet? TryShoot(float playerX, float playerY)
        {
            if (_fireTimer > 0f) return null;
            _fireTimer = FireInterval;
            return new Bullet(X + Width / 2f - 3, Y + Height, 0f, 1f, speed: 260f, damage: 10, owner: BulletOwner.Enemy);
        }

        public override void Draw(Graphics g)
        {
            DrawSprite(g, "shooter.png");
        }
    }
}
