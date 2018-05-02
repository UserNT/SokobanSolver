using System;
using System.Collections.Generic;
using System.Linq;

namespace Sokoban
{
    public class CanMoveBox
    {
        public CanMoveBox(Cell box, Direction PushDirection, List<Cell> path)
        {
            Box = box;
            this.PushDirection = PushDirection;
            Path = path;
        }

        public Cell Box { get; }

        public Direction PushDirection { get; }

        public List<Cell> Path { get; }

        public static IEnumerable<CanMoveBox> Find(GameData gameData)
        {
            var list = new List<CanMoveBox>();

            var testedCells = new List<string>();

            Populate(gameData, gameData.KeeperCell, list, testedCells, null);

            return list;
        }

        private static void Populate(GameData gameData, Cell currentCell, List<CanMoveBox> canMoveBoxList, List<string> testedCells, List<Cell> path)
        {
            if (path == null)
            {
                path = new List<Cell>();
            }

            path.Add(currentCell);
            testedCells.Add(GetCellId(currentCell));

            var canMoveBoxes = GetCanMoveBox(gameData, currentCell, path);
            canMoveBoxList.AddRange(canMoveBoxes);

            var emptyCells = GetEmptyCells(gameData, currentCell, testedCells);

            foreach (var cell in emptyCells)
            {
                Populate(gameData, cell, canMoveBoxList, testedCells, path.ToList());
            }
        }

        private static IEnumerable<CanMoveBox> GetCanMoveBox(GameData gameData, Cell currentCell, List<Cell> path)
        {
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                int xOffset = direction == Direction.Left ? -1 :
                              direction == Direction.Up ? 0 :
                              direction == Direction.Right ? 1 : 0;

                int yOffset = direction == Direction.Left ? 0 :
                              direction == Direction.Up ? -1 :
                              direction == Direction.Right ? 0 : 1;

                var x = currentCell.X + xOffset;
                var y = currentCell.Y + yOffset;

                var cellTypeAtOffset = gameData.Map[x, y];

                if (cellTypeAtOffset == CellType.BOX || cellTypeAtOffset == CellType.BOX_ON_LOCATION)
                {
                    var boxCell = new Cell(x, y, cellTypeAtOffset);

                    var localPath = path.ToList();
                    localPath.Add(boxCell);

                    if (gameData.CanKeeperMoveBox(localPath[localPath.Count - 2], localPath[localPath.Count - 1]))
                    {
                        yield return new CanMoveBox(boxCell, direction, localPath);
                    }
                }
            }
        }

        private static IEnumerable<Cell> GetEmptyCells(GameData gameData, Cell currentCell, List<string> testedCells)
        {
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                int xOffset = direction == Direction.Left ? -1 :
                              direction == Direction.Up ? 0 :
                              direction == Direction.Right ? 1 : 0;

                int yOffset = direction == Direction.Left ? 0 :
                              direction == Direction.Up ? -1 :
                              direction == Direction.Right ? 0 : 1;

                var x = currentCell.X + xOffset;
                var y = currentCell.Y + yOffset;

                if (testedCells.Contains(GetCellId(x, y)))
                {
                    continue;
                }

                var cellTypeAtOffset = gameData.Map[x, y];

                if (cellTypeAtOffset == CellType.EMPTY || cellTypeAtOffset == CellType.LOCATION)
                {
                    yield return new Cell(x, y, cellTypeAtOffset);
                }
            }
        }

        private static string GetCellId(Cell cell)
        {
            return GetCellId(cell.X, cell.Y);
        }

        private static string GetCellId(int x, int y)
        {
            return string.Format("{0}_{1}", x, y);
        }
    }
}
