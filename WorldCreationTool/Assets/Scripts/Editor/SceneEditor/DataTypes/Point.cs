namespace Editor.SceneEditor.DataTypes {
    public struct Point {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y) {
            X = x;
            Y = y;
        }

        public static Point operator +(Point a, Point b) {
            return new Point(a.X + b.X, a.Y + b.Y);
        }
        
        public static Point operator -(Point a, Point b) {
            return new Point(a.X - b.X, a.Y - b.Y);
        }

        public static Point operator /(Point a, float div) {
            return new Point((int)(a.X / div), (int)(a.Y / div));
        }
        
        public static Point operator *(Point a, float mul) {
            return new Point((int)(a.X * mul), (int)(a.Y * mul));
        }

        public static bool operator ==(Point lhs, Point rhs) {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        public static bool operator !=(Point lhs, Point rhs) {
            return !(lhs == rhs);
        }
    }
}
