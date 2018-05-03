namespace Sokoban.Network
{
    public class CellStaticInfo
    {
        private CellStaticInfo left;
        private CellStaticInfo top;
        private CellStaticInfo right;
        private CellStaticInfo bottom;

        public CellStaticInfo(int position, int x, int y, bool isLocation, bool isWall)
        {
            Position = position;
            X = x;
            Y = y;
            IsLocation = isLocation;
            IsWall = isWall;
        }

        public int Position { get; }

        public int X { get; }

        public int Y { get; }

        public bool IsLocation { get; }

        public bool IsWall { get; }

        public CellStaticInfo Left
        {
            get => left;
            set => left = (left == null) ? value : left;
        }

        public CellStaticInfo Top
        {
            get => top;
            set => top = (top == null) ? value : top;
        }

        public CellStaticInfo Right
        {
            get => right;
            set => right = (right == null) ? value : right;
        }

        public CellStaticInfo Bottom
        {
            get => bottom;
            set => bottom = (bottom == null) ? value : bottom;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}); {2}", X, Y, IsLocation ? "Location" : IsWall ? "Wall" : "");
        }
    }
}
