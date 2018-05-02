using System;

namespace Sokoban.AStar
{
    public class KeeperPathNode : Node<Position>
    {
        public KeeperPathNode(Position cell, bool isWalkable, Position targetCell)
            : base(cell, isWalkable, targetCell)
        {
        }

        public override double GetTraversalCost(Position toCell)
        {
            var location = this.Cell;
            var otherLocation = toCell;

            double deltaX = otherLocation.X - location.X;
            double deltaY = otherLocation.Y - location.Y;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
