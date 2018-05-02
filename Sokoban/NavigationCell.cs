using System;

namespace Sokoban
{
    public class NavigationCell
    {
        public NavigationCell(CellType cellType)
        {
            switch (cellType)
            {
                case CellType.WALL:
                    IsWall = true;
                    isCorner = false;
                    isForbiddenForBox = true;
                    break;
                case CellType.LOCATION:
                case CellType.KEEPER_ON_LOCATION:
                case CellType.BOX_ON_LOCATION:
                    IsLocation = true;
                    isForbiddenForBox = false;
                    break;
                case CellType.BOX:
                    isCorner = false;
                    isForbiddenForBox = false;
                    break;
                default:
                    break;
            }
        }

        public NavigationCell Left { get; set; }

        public NavigationCell Up { get; set; }

        public NavigationCell Right { get; set; }

        public NavigationCell Down { get; set; }

        public bool IsWall { get; }

        public bool IsLocation { get; }

        private bool? isCorner;
        public bool IsCorner
        {
            get
            {
                if (!isCorner.HasValue)
                {
                    isCorner = ((Left != null && Left.IsWall && Up != null && Up.IsWall) ||
                                (Up != null && Up.IsWall && Right != null && Right.IsWall) ||
                                (Right != null && Right.IsWall && Down != null && Down.IsWall) ||
                                (Down != null && Down.IsWall && Left != null && Left.IsWall));
                }

                return isCorner.Value;
            }
        }

        private bool? isForbiddenForBox;
        public bool IsForbiddenForBox
        {
            get
            {
                if (IsCorner)
                {
                    isForbiddenForBox = true;
                }

                if (!isForbiddenForBox.HasValue)
                {
                    isForbiddenForBox = ((Left != null && Left.IsWall && IsEndsWithCorner(x => x.Up, x => x.Left) && IsEndsWithCorner(x => x.Down, x => x.Left)) ||
                                        (Up != null && Up.IsWall && IsEndsWithCorner(x => x.Left, x => x.Up) && IsEndsWithCorner(x => x.Right, x => x.Up)) ||
                                        (Right != null && Right.IsWall && IsEndsWithCorner(x => x.Up, x => x.Right) && IsEndsWithCorner(x => x.Down, x => x.Right)) ||
                                        (Down != null && Down.IsWall && IsEndsWithCorner(x => x.Left, x => x.Down) && IsEndsWithCorner(x => x.Right, x => x.Down)));
                }

                return isForbiddenForBox.Value;
            }
        }

        private bool IsEndsWithCorner(Func<NavigationCell, NavigationCell> getNextCell, Func<NavigationCell, NavigationCell> getExpectedWallCell)
        {
            var currentCell = getNextCell(this);

            while (currentCell != null && !currentCell.IsWall)
            {
                if (currentCell.IsLocation)
                {
                    return false;
                }

                var expectedWallCell = getExpectedWallCell(currentCell);

                if (expectedWallCell == null || !expectedWallCell.IsWall)
                {
                    return false;
                }

                currentCell = getNextCell(currentCell);
            }

            return true;
        }
    }
}
