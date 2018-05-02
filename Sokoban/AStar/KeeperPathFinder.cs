using System.Collections.Generic;
using System.Windows;

namespace Sokoban.AStar
{
    public class KeeperPathFinder : PathFinder<Position>
    {
        private readonly SokobanMap map;

        public KeeperPathFinder(SokobanMap map)
            : base()
        {
            this.map = map;
        }

        protected override int GetWidth()
        {
            return map.Width;
        }

        protected override int GetHeight()
        {
            return map.Height;
        }

        protected override Node<Position> CreateNode(int x, int y, Position targetCell)
        {
            var cell = new Position(x, y);

            return new KeeperPathNode(cell, IsWalkable(cell), targetCell);
        }

        protected override Point GetCellLocation(Position cell)
        {
            return new Point(cell.X, cell.Y);
        }

        protected override IEnumerable<Position> GetAdjacentCells(Position cell, int depth)
        {
            var neighbors = new List<Position>();

            foreach (var key in SokobanMap.SupportedKeys)
            {
                var targetPos = map.GetPosition(key, cell);
                var pos = new Position(targetPos, map.Width, map.Height);

                if (IsWalkable(pos))
                {
                    neighbors.Add(pos);
                }
            }

            return neighbors;
        }

        private bool IsWalkable(Position pos)
        {
            return map[pos] == SokobanMap.EMPTY || map[pos] == SokobanMap.LOCATION;
        }
    }
}
