namespace Sokoban
{
    public class Position
    {
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Position(int pos, int width, int height)
        {
            Y = pos / width;
            X = pos - Y * width;
        }

        public int X { get; }

        public int Y { get; }

        public override string ToString()
        {
            return string.Format("X: {0}; Y: {1};", X, Y);
        }
    }
}
