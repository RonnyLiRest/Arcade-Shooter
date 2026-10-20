using System.Drawing;

namespace ArcadeShooter.Managers
{
    /// <summary>
    /// Статический класс-помощник: не хранит состояние, только
    /// проверяет пересечение прямоугольников (AABB collision).
    /// </summary>
    public static class CollisionManager
    {
        public static bool Intersects(RectangleF a, RectangleF b) => a.IntersectsWith(b);
    }
}
