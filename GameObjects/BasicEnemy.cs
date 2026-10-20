using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    /// <summary>Простейший враг: летит прямо вниз с постоянной скоростью.</summary>
    public class BasicEnemy : Enemy
    {
        public BasicEnemy(float x, float y)
            : base(x, y, width: 47, height: 40, maxHealth: 2, speed: 90f, scoreValue: 10)
        {
        }

        protected override void Move(float deltaTime)
        {
            Y += Speed * deltaTime;
        }

        public override void Draw(Graphics g)
        {
            DrawSprite(g, "basic.png");
        }
    }
}
