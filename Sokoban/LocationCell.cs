using System;
using System.Collections.Generic;
using System.Linq;

namespace Sokoban
{
    public class LocationCell : Cell
    {
        public LocationCell(int x, int y) :
            base(x, y, CellType.LOCATION)
        {
            CanBeClosedFrom = new List<Direction>();
        }

        public List<Direction> CanBeClosedFrom { get; private set; }

        public static List<LocationCell> GetLocationCellsToClose(GameData gameData)
        {
            var cells = new List<LocationCell>();

            foreach (var currentCell in gameData.LocationCells)
            {
                var locationCell = new LocationCell(currentCell.X, currentCell.Y);

                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                {
                    int xOffset = direction == Direction.Left ? -1 :
                                  direction == Direction.Up ? 0 :
                                  direction == Direction.Right ? 1 : 0;

                    int yOffset = direction == Direction.Left ? 0 :
                                  direction == Direction.Up ? -1 :
                                  direction == Direction.Right ? 0 : 1;


                    var x1 = currentCell.X + xOffset;
                    var y1 = currentCell.Y + yOffset;

                    var cellType1 = gameData.Map[x1, y1];
                    if (cellType1 == CellType.WALL || cellType1 == CellType.BOX || cellType1 == CellType.BOX_ON_LOCATION) continue;

                    var x2 = currentCell.X + 2 * xOffset;
                    if (x2 < 0 || x2 >= gameData.Width) continue;

                    var y2 = currentCell.Y + 2 * yOffset;
                    if (y2 < 0 || y2 >= gameData.Height) continue;

                    var cellType2 = gameData.Map[x2, y2];
                    if (cellType2 == CellType.WALL || cellType2 == CellType.BOX || cellType2 == CellType.BOX_ON_LOCATION) continue;

                    //if ((cellType1 == CellType.EMPTY || cellType1 == CellType.LOCATION || cellType1 == CellType.KEEPER || cellType1 == CellType.KEEPER_ON_LOCATION) &&
                    //    (cellType2 == CellType.EMPTY || cellType2 == CellType.LOCATION || cellType2 == CellType.KEEPER || cellType2 == CellType.KEEPER_ON_LOCATION))
                    //{
                    locationCell.CanBeClosedFrom.Add(direction);
                    //}
                }

                if (locationCell.CanBeClosedFrom.Count > 0)
                {
                    cells.Add(locationCell);
                }
            }

            return cells.OrderBy(c => c.CanBeClosedFrom.Count)
                        .ToList();

            //return cells.GroupBy(c => c.CanBeClosedFrom.Count)
            //            .OrderBy(g => g.Key)
            //            .FirstOrDefault()
            //            .ToList();
        }
    }
}
