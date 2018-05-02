using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Sokoban
{
    public class GameData
    {
        private readonly List<Cell> locations = new List<Cell>();
        private readonly List<Cell> boxes = new List<Cell>();

        private GameData initialGameData;

        private GameData(CellType[,] map)
        {
            Width = map.GetLength(0);
            Height = map.GetLength(1);

            Map = map.Clone() as CellType[,];

            for (int column = 0; column < Width; column++)
            {
                for (int row = 0; row < Height; row++)
                {
                    var cellType = Map[column, row];

                    if (cellType == CellType.KEEPER)
                    {
                        KeeperCell = new Cell(column, row, CellType.KEEPER);
                    }
                    else if (cellType == CellType.BOX)
                    {
                        boxes.Add(new Cell(column, row, CellType.BOX));
                    }
                }
            }
        }

        public GameData(string gameData)
        {
            var parts = gameData.Split('*');

            Height = int.Parse(parts[0]);
            Width = int.Parse(parts[1]);

            Map = new CellType[Width, Height];
            BoxNavigationMap = new NavigationCell[Width, Height];

            int row = 0;
            int column = 0;
            for (int i = 0; i < parts[2].Length; i++)
            {
                if (i > 0 && i % Width == 0)
                {
                    row++;
                    column = 0;
                }

                var cellType = (CellType)int.Parse(parts[2][i].ToString());
                Map[column, row] = cellType;

                if (cellType == CellType.KEEPER)
                {
                    KeeperCell = new Cell(column, row, CellType.KEEPER);
                }
                else if (cellType == CellType.LOCATION)
                {
                    locations.Add(new Cell(column, row, CellType.LOCATION));
                }
                else if (cellType == CellType.BOX)
                {
                    boxes.Add(new Cell(column, row, CellType.BOX));
                }

                var currentCell = new NavigationCell(cellType);
                BoxNavigationMap[column, row] = currentCell;

                int leftX = column - 1;
                if (leftX >= 0)
                {
                    var leftCell = BoxNavigationMap[leftX, row];
                    leftCell.Right = currentCell;
                    currentCell.Left = leftCell;
                }

                int upY = row - 1;
                if (upY >= 0)
                {
                    var upCell = BoxNavigationMap[column, upY];
                    upCell.Down = currentCell;
                    currentCell.Up = upCell;
                }

                column++;
            }

            initialGameData = this;
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public CellType[,] Map { get; private set; }

        public Cell KeeperCell { get; private set; }

        public IEnumerable<Cell> LocationCells
        {
            get { return initialGameData.locations; }
        }

        public IEnumerable<Cell> BoxCells
        {
            get { return boxes; }
        }

        public NavigationCell[,] BoxNavigationMap { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int row = 0; row < Height; row++)
            {
                for (int column = 0; column < Width; column++)
                {
                    var currentCell = initialGameData.BoxNavigationMap[column, row];

                    var c = " ";

                    if (currentCell.IsWall)
                    {
                        c = "W";
                    }
                    else if (currentCell.IsLocation)
                    {
                        c = "L";
                    }
                    else if (currentCell.IsCorner)
                    {
                        c = "C";
                    }
                    else if (currentCell.IsForbiddenForBox)
                    {
                        c = "F";
                    }

                    sb.Append(c);
                }

                sb.AppendLine();
            }

            var str = sb.ToString();
            Clipboard.SetText(str);
            return str;
        }

        public static Cell GetTargetCell(Cell keeperCell, Cell boxCell, CellType[,] map)
        {
            var xOffset = boxCell.X - keeperCell.X;
            var yOffset = boxCell.Y - keeperCell.Y;

            var targetX = boxCell.X + xOffset;
            var targetY = boxCell.Y + yOffset;

            var targetCellType = map[targetX, targetY];

            return new Cell(targetX, targetY, targetCellType);
        }

        public static bool CanKeeperMoveBox(Cell keeperCell, Cell boxCell, GameData gameData)
        {
            var targetCell = GetTargetCell(keeperCell, boxCell, gameData.Map);

            var navigationCell = gameData.initialGameData.BoxNavigationMap[targetCell.X, targetCell.Y];
            if (navigationCell.IsForbiddenForBox)
            {
                return false;
            }

            return targetCell.CellType == CellType.EMPTY ||
                   targetCell.CellType == CellType.LOCATION;
        }

        public bool CanKeeperMoveBox(Cell keeperCell, Cell boxCell)
        {
            return CanKeeperMoveBox(keeperCell, boxCell, this);
        }

        public GameData MakeMove(Cell[] path)
        {
            var resultMap = Map.Clone() as CellType[,];

            var keeperCell = path[0];
            var boxCell = path[path.Length - 1];
            var targetCell = GetTargetCell(path[path.Length - 2], boxCell, Map);

            resultMap[keeperCell.X, keeperCell.Y] = initialGameData.Map[keeperCell.X, keeperCell.Y] == CellType.LOCATION ? CellType.LOCATION : CellType.EMPTY;
            resultMap[boxCell.X, boxCell.Y] = initialGameData.Map[boxCell.X, boxCell.Y] == CellType.LOCATION ? CellType.KEEPER_ON_LOCATION : CellType.KEEPER;
            resultMap[targetCell.X, targetCell.Y] = initialGameData.Map[targetCell.X, targetCell.Y] == CellType.LOCATION ? CellType.BOX_ON_LOCATION : CellType.BOX;

            var gameData = new GameData(resultMap);
            gameData.initialGameData = this.initialGameData;

            return gameData;
        }

        public bool IsComplete()
        {
            return initialGameData.LocationCells.Any(locationCell => Map[locationCell.X, locationCell.Y] == CellType.BOX_ON_LOCATION);
            //return initialGameData.LocationCells.All(locationCell => Map[locationCell.X, locationCell.Y] == CellType.BOX_ON_LOCATION);
        }
    }
}
