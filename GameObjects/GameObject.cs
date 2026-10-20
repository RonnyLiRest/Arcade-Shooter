using System.Drawing;
using ArcadeShooter.Managers;

namespace ArcadeShooter.GameObjects
{
    /// <summary>
    /// Базовый абстрактный класс для всех объектов игрового поля.
    /// Хранит положение, размеры и признак активности (используется
    /// вместо удаления объекта из списка на каждом кадре).
    /// </summary>
    public abstract class GameObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }

        /// <summary>Если false — объект должен быть удалён из игры на следующем кадре.</summary>
        public bool IsActive { get; set; } = true;

        protected GameObject(float x, float y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Прямоугольник для проверки столкновений (AABB).</summary>
        public RectangleF Bounds => new RectangleF(X, Y, Width, Height);

        /// <summary>Обновление логики объекта за один тик игрового цикла.</summary>
        public abstract void Update(float deltaTime);

        /// <summary>Отрисовка объекта на поверхности формы.</summary>
        public abstract void Draw(Graphics g);

        /// <summary>
        /// Рисует спрайт из Assets/Images по имени файла, растягивая его на
        /// прямоугольник объекта (X, Y, Width, Height). Этот же прямоугольник
        /// используется как хитбокс (Bounds), поэтому картинка и столкновения совпадают.
        /// </summary>
        protected void DrawSprite(Graphics g, string spriteFileName)
        {
            g.DrawImage(SpriteManager.Get(spriteFileName), X, Y, Width, Height);
        }
    }
}
