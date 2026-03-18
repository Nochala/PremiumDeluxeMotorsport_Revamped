using System.Collections.Generic;
using GTA.Math;

namespace PDMCD4
{
    public class Interior
    {
        public List<Vector2> Points { get; } = new List<Vector2>();

        public void Add(Vector3 pt)
        {
            Points.Add(new Vector2(pt.X, pt.Y));
        }

        public bool IsInInterior(Vector3 position)
        {
            bool inside = false;
            int j = Points.Count - 1;

            for (int i = 0; i < Points.Count; i++)
            {
                bool intersects =
                    ((Points[i].Y > position.Y) != (Points[j].Y > position.Y)) &&
                    (position.X < (Points[j].X - Points[i].X) * (position.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) + Points[i].X);

                if (intersects)
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }
    }

    public class Circle
    {
        public Vector3 Start { get; set; }
        public float Radius { get; set; }

        public Circle(Vector3 start, float radius)
        {
            Start = start;
            Radius = radius;
        }

        public bool Intersects(Vector3 pt)
        {
            Vector2 a = new Vector2(pt.X, pt.Y);
            Vector2 b = new Vector2(Start.X, Start.Y);
            return a.DistanceTo(b) <= Radius;
        }
    }
}
