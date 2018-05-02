namespace Sokoban
{
    public class Cell
    {
        public Cell(int x, int y, CellType cellType)
        {
            X = x;
            Y = y;
            CellType = cellType;
        }

        public int X { get; private set; }

        public int Y { get; private set; }

        public CellType CellType { get; private set; }

        public override string ToString()
        {
            return string.Format("X: {0}; Y: {1}; {2}", X, Y, CellType);
        }
    }
}
