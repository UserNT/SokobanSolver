using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Sokoban.AStar
{
    public class BoxPathFinder : PathFinder<Position>
    {
        private readonly SokobanMap map;

        public BoxPathFinder(SokobanMap map)
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

            bool isWalkable = false;
            if (x > 0 && y > 0 && x < map.Width - 1 && y < map.Height - 1)
            {
                isWalkable = map.CanMoveBox(map.Convert(cell));
            }

            return new KeeperPathNode(cell, isWalkable, targetCell);
        }

        protected override IEnumerable<Position> GetAdjacentCells(Position cell, int depth)
        {
            var neighbors = new List<Position>();

            var initialKeeperPos = map.GetKeeperPosition();
            var boxPosition = map.Convert(cell);

            Position newBoxPosition;
            if (CanMoveBox(Key.Left, Key.Right, initialKeeperPos, boxPosition, out newBoxPosition))
            {
                neighbors.Add(newBoxPosition);
            }

            if (CanMoveBox(Key.Right, Key.Left, initialKeeperPos, boxPosition, out newBoxPosition))
            {
                neighbors.Add(newBoxPosition);
            }

            if (CanMoveBox(Key.Up, Key.Down, initialKeeperPos, boxPosition, out newBoxPosition))
            {
                neighbors.Add(newBoxPosition);
            }

            if (CanMoveBox(Key.Down, Key.Up, initialKeeperPos, boxPosition, out newBoxPosition))
            {
                neighbors.Add(newBoxPosition);
            }

            return neighbors;
        }

        protected override Point GetCellLocation(Position cell)
        {
            return new Point(cell.X, cell.Y);
        }

        private bool CanMoveBox(Key from, Key to, int initialKeeperPos, int boxPosition, out Position newBoxPosition)
        {
            var toSide = map.GetPosition(to, boxPosition);
            newBoxPosition = map.Convert(toSide);
            int tmp;
            if (!map.CanMoveBox(to, boxPosition, out tmp))
            {
                return false;
            }

            var fromSide = map.GetPosition(from, boxPosition);
            if (fromSide != map.Convert(startNode.Cell))
            {
                if (map[fromSide] != SokobanMap.EMPTY &&
                    map[fromSide] != SokobanMap.LOCATION &&
                    map[fromSide] != SokobanMap.KEEPER &&
                    map[fromSide] != SokobanMap.KEEPER_ON_LOCATION)
                {
                    return false;
                }

                var keeperPathFinder = new KeeperPathFinder(map);
                var pathToBoxSide = keeperPathFinder.FindPath(map.Convert(initialKeeperPos), map.Convert(fromSide));

                return pathToBoxSide.Length > 0;
            }
            else
            {
                return true;
            }
        }
    }
}
