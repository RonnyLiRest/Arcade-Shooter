using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    public enum BulletOwner
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Снаряд. Используется и игроком, и врагами — различие задаётся
    /// полем Owner (кого поражает снаряд) и направлением полёта.
    /// </summary>
    public class Bullet : GameObject
    {
        public BulletOwner Owner { get; }
        public int Damage { get; }

        private readonly float _dirX;
        private readonly float _dirY;
        private readonly float _speed;

        public Bullet(float x, float y, float dirX, float dirY, float speed, int damage, BulletOwner owner)
            : base(x, y, 6, 12)
        {
            _dirX = dirX;
            _dirY = dirY;
            _speed = speed;
            Damage = damage;
            Owner = owner;
        }

        public override void Update(float deltaTime)
        {
            X += _dirX * _speed * deltaTime;
            Y += _dirY * _speed * deltaTime;
        }

        public override void Draw(Graphics g)
        {
            using var brush = new SolidBrush(Owner == BulletOwner.Player ? Color.Yellow : Color.OrangeRed);
            g.FillEllipse(brush, X, Y, Width, Height);
        }
    }
}
