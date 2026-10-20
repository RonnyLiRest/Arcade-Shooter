using System.Collections.Generic;
using System.Drawing;

namespace ArcadeShooter.GameObjects
{
    /// <summary>
    /// Игровой объект, управляемый пользователем.
    /// Отвечает за движение по полю и стрельбу вверх по врагам.
    /// </summary>
    public class Player : Entity
    {
        public int Score { get; set; }
        public int Lives { get; set; }

        /// <summary>Активен ли сейчас бафф RapidFire (двойной залп + ускорение).</summary>
        public bool IsRapidFireActive => _rapidFireTimer > 0f;

        /// <summary>Сколько секунд баффа RapidFire осталось — для кругового таймера в HUD.</summary>
        public float RapidFireRemainingSeconds => _rapidFireTimer;

        /// <summary>Полная длительность текущего баффа RapidFire — для расчёта доли круга.</summary>
        public float RapidFireDurationSeconds { get; private set; }

        private const float BaseFireCooldown = 0.28f; // секунд между выстрелами
        private const float RapidFireRateMultiplier = 1.35f;
        private const float RapidFireSpeedMultiplier = 1.2f;

        private float _cooldownTimer;
        private float _rapidFireTimer;
        private float _fireRateMultiplier = 1f;
        private float _speedMultiplier = 1f;

        public Player(float x, float y)
            : base(x, y, 50, 50, maxHealth: 100, speed: 260f)
        {
            Lives = 3;
        }

        public void MoveBy(float dx, float dy, float deltaTime, Rectangle bounds)
        {
            float effectiveSpeed = Speed * _speedMultiplier;
            X += dx * effectiveSpeed * deltaTime;
            Y += dy * effectiveSpeed * deltaTime;

            X = System.Math.Clamp(X, bounds.Left, bounds.Right - Width);
            Y = System.Math.Clamp(Y, bounds.Top, bounds.Bottom - Height - 10);
        }

        /// <summary>
        /// Пытается выстрелить. Возвращает список снарядов: пустой, если ещё
        /// не перезарядился, один — в обычном режиме, два — при активном RapidFire.
        /// </summary>
        public List<Bullet> TryShoot()
        {
            var result = new List<Bullet>();
            if (_cooldownTimer > 0f) return result;

            _cooldownTimer = BaseFireCooldown / _fireRateMultiplier;

            if (IsRapidFireActive)
            {
                // двойной залп — два снаряда чуть левее и правее центра корабля
                result.Add(new Bullet(X + Width / 2f - 12, Y - 12, 0f, -1f, speed: 520f, damage: 1, owner: BulletOwner.Player));
                result.Add(new Bullet(X + Width / 2f + 6, Y - 12, 0f, -1f, speed: 520f, damage: 1, owner: BulletOwner.Player));
            }
            else
            {
                result.Add(new Bullet(X + Width / 2f - 3, Y - 12, 0f, -1f, speed: 520f, damage: 1, owner: BulletOwner.Player));
            }

            return result;
        }

        /// <summary>Активирует бафф RapidFire: двойной залп + небольшое ускорение, на заданное время.</summary>
        public void ActivateRapidFire(float durationSeconds)
        {
            _rapidFireTimer = durationSeconds;
            RapidFireDurationSeconds = durationSeconds;
            _fireRateMultiplier = RapidFireRateMultiplier;
            _speedMultiplier = RapidFireSpeedMultiplier;
        }

        /// <summary>Выключает бафф RapidFire: возвращает скорострельность и скорость к обычным.</summary>
        public void DeactivateRapidFire()
        {
            _rapidFireTimer = 0f;
            _fireRateMultiplier = 1f;
            _speedMultiplier = 1f;
        }

        public void Respawn(Rectangle bounds)
        {
            X = bounds.Left + bounds.Width / 2f - Width / 2f;
            Y = bounds.Bottom - Height - 20;
            Health = MaxHealth;
            IsActive = true;
            DeactivateRapidFire(); // потеря жизни сбрасывает бафф стрельбы
        }

        public override void Update(float deltaTime)
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= deltaTime;

            if (_rapidFireTimer > 0f)
            {
                _rapidFireTimer -= deltaTime;
                if (_rapidFireTimer <= 0f) DeactivateRapidFire();
            }
        }

        public override void Draw(Graphics g)
        {
            DrawSprite(g, "player.png");

            // полоска здоровья над кораблём
            float ratio = Health / (float)MaxHealth;
            using var hpBack = new SolidBrush(Color.DimGray);
            using var hpFront = new SolidBrush(Color.LimeGreen);
            g.FillRectangle(hpBack, X, Y + Height + 4, Width, 5);
            g.FillRectangle(hpFront, X, Y + Height + 4, Width * ratio, 5);
        }
    }
}
