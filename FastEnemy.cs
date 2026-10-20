using System;
using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    /// <summary>Быстрый враг, движущийся вниз змейкой (синусоида по X).</summary>
    public class FastEnemy : Enemy
    {
        private float _time;
        private readonly float _baseX;

        public FastEnemy(float x, float y)
            : base(x, y, width: 41, height: 32, maxHealth: 1, speed: 180f, scoreValue: 20)
        {
            _baseX = x;
        }

        protected override void Move(float deltaTime)
        {
            _time += deltaTime;
            Y += Speed * deltaTime;
            X = _baseX + (float)Math.Sin(_time * 4f) * 60f;
        }

        public override void Draw(Graphics g)
        {
            DrawSprite(g, "fast.png");
        }
    }
}
