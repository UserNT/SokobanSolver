using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Sokoban
{
    public class SokobanMap
    {
        public static readonly Key[] SupportedKeys = new Key[] { Key.Left, Key.Up, Key.Right, Key.Down };

        public const char EMPTY = '0';
        public const char KEEPER = '1';
        public const char BOX = '2';
        public const char WALL = '3';
        public const char LOCATION = '4';
        public const char BOX_ON_LOCATION = '5';
        public const char KEEPER_ON_LOCATION = '6';

        private string map;

        public SokobanMap(int width, int height, string map)
        {
            Width = width;
            Height = height;

            this.map = map;
        }

        public int Width { get; }

        public int Height { get; }

        public bool IsComplete()
        {
            var anyBoxOrLocation = map.Any(x => x == BOX || x == LOCATION);

            return !anyBoxOrLocation;
        }

        public int GetKeeperPosition()
        {
            return map.IndexOfAny(new[] { KEEPER, KEEPER_ON_LOCATION });
        }

        public bool SetKeeperPosition(int newKeeperPos, int currentKeeperPos = -1)
        {
            if (currentKeeperPos == -1)
            {
                currentKeeperPos = GetKeeperPosition();
            }

            if (map[newKeeperPos] == WALL || map[newKeeperPos] == BOX || map[newKeeperPos] == BOX_ON_LOCATION)
            {
                return false;
            }

            var sb = new StringBuilder(map);
            sb[newKeeperPos] = (sb[newKeeperPos] == EMPTY) ? KEEPER : KEEPER_ON_LOCATION;
            map = sb.ToString();

            return true;
        }

        public char this[int pos]
        {
            get { return map[pos]; }
        }

        public char this[Position pos]
        {
            get { return this[Convert(pos)]; }
        }

        public int Convert(Position pos)
        {
            return pos.Y * Width + pos.X;
        }

        public Position Convert(int pos)
        {
            return new Position(pos, Width, Height);
        }

        public int GetPosition(Key key, Position currentPos)
        {
            var pos = Convert(currentPos);

            return GetPosition(key, pos);
        }

        public int GetPosition(Key key, int currentPos)
        {
            if (key == Key.Left)
            {
                return (currentPos % Width == 0) ? -1 : currentPos - 1;
            }
            else if (key == Key.Right)
            {
                return ((currentPos + 1) % Width == 0) ? -1 : currentPos + 1;
            }
            else if (key == Key.Up)
            {
                return (currentPos - Width < 0) ? -1 : currentPos - Width;
            }
            else if (key == Key.Down)
            {
                return (currentPos + Width >= map.Length) ? -1 : currentPos + Width;
            }

            return -1;
        }

        public List<int> FindBoxPositions()
        {
            var positions = new List<int>();

            var cellTypes = new[] { BOX, BOX_ON_LOCATION };

            int index = 0;
            while (true)
            {
                var boxIndex = map.IndexOfAny(cellTypes, index);
                if (boxIndex == -1)
                {
                    break;
                }
                else
                {
                    positions.Add(boxIndex);

                    index = boxIndex + 1;
                }
            }

            return positions;
        }

        public List<SokobanPathItem> FindEntryPoints()
        {
            var points = new List<SokobanPathItem>();

            var cellTypes = new[] { LOCATION, KEEPER_ON_LOCATION, BOX_ON_LOCATION };

            var index = 0;
            while (true)
            {
                var locationIndex = map.IndexOfAny(cellTypes, index);
                if (locationIndex == -1)
                {
                    break;
                }
                else
                {
                    int fromPosition;

                    if (IsEntryPoint(Key.Left, locationIndex, out fromPosition))
                    {
                        points.Add(new SokobanPathItem() { Position = fromPosition, Key = Key.Right, StepsToTarget = 1 });
                    }

                    if (IsEntryPoint(Key.Up, locationIndex, out fromPosition))
                    {
                        points.Add(new SokobanPathItem() { Position = fromPosition, Key = Key.Down, StepsToTarget = 1 });
                    }

                    if (IsEntryPoint(Key.Right, locationIndex, out fromPosition))
                    {
                        points.Add(new SokobanPathItem() { Position = fromPosition, Key = Key.Left, StepsToTarget = 1 });
                    }

                    if (IsEntryPoint(Key.Down, locationIndex, out fromPosition))
                    {
                        points.Add(new SokobanPathItem() { Position = fromPosition, Key = Key.Up, StepsToTarget = 1 });
                    }

                    index = locationIndex + 1;
                }
            }

            return points;
        }

        private bool IsEntryPoint(Key key, int locationPosition, out int fromPosition)
        {
            fromPosition = GetPosition(key, locationPosition);

            if (fromPosition == -1 || map[fromPosition] == WALL)
            {
                return false;
            }

            var fromPosition2 = GetPosition(key, fromPosition);

            if (fromPosition2 == -1 || map[fromPosition2] == WALL)
            {
                return false;
            }

            return map[fromPosition] == EMPTY ||
                   map[fromPosition] == KEEPER ||
                   map[fromPosition] == BOX;
        }

        public bool CanMoveBox(int newBoxPosition)
        {
            if (newBoxPosition == -1)
            {
                return false;
            }

            var cellTypeAtNewPosition = map[newBoxPosition];
            if (cellTypeAtNewPosition == BOX ||
                cellTypeAtNewPosition == BOX_ON_LOCATION ||
                cellTypeAtNewPosition == WALL)
            {
                return false;
            }

            //if (IsLeftTopCorner(newBoxPosition) ||
            //    IsLeftBottomCorner(newBoxPosition) ||
            //    IsRightTopCorner(newBoxPosition) ||
            //    IsRightBottomCorner(newBoxPosition))
            //{
            //    return false;
            //}

            return true;
        }

        public bool CanMoveBox(Key key, int boxPosition, out int newBoxPosition)
        {
            newBoxPosition = GetPosition(key, boxPosition);
            if (newBoxPosition == -1)
            {
                return false;
            }

            var cellTypeAtNewPosition = map[newBoxPosition];
            if (cellTypeAtNewPosition == BOX ||
                cellTypeAtNewPosition == BOX_ON_LOCATION ||
                cellTypeAtNewPosition == WALL)
            {
                return false;
            }

            if (key == Key.Left)
            {
                if (IsLeftTopCorner(newBoxPosition) || IsLeftBottomCorner(newBoxPosition))
                {
                    return false;
                }
            }
            else if (key == Key.Up)
            {
                if (IsLeftTopCorner(newBoxPosition) || IsRightTopCorner(newBoxPosition))
                {
                    return false;
                }
            }
            else if (key == Key.Right)
            {
                if (IsRightTopCorner(newBoxPosition) || IsRightBottomCorner(newBoxPosition))
                {
                    return false;
                }
            }
            else if (key == Key.Down)
            {
                if (IsLeftBottomCorner(newBoxPosition) || IsRightBottomCorner(newBoxPosition))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsLeftTopCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var top = GetPosition(Key.Up, position);
            var leftTop = GetPosition(Key.Up, left);

            return (map[left] == WALL || map[left] == BOX) &&
                   (map[top] == WALL || map[top] == BOX) &&
                   (map[leftTop] == WALL || map[leftTop] == BOX);
        }

        private bool IsRightTopCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var top = GetPosition(Key.Up, position);
            var rightTop = GetPosition(Key.Up, right);

            return (map[right] == WALL || map[right] == BOX) &&
                   (map[top] == WALL || map[top] == BOX) &&
                   (map[rightTop] == WALL || map[rightTop] == BOX);
        }

        private bool IsLeftBottomCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var bottom = GetPosition(Key.Down, position);
            var leftBottom = GetPosition(Key.Down, left);

            return (map[left] == WALL || map[left] == BOX) &&
                   (map[bottom] == WALL || map[bottom] == BOX) &&
                   (map[leftBottom] == WALL || map[leftBottom] == BOX);
        }

        private bool IsRightBottomCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var bottom = GetPosition(Key.Down, position);
            var rightBottom = GetPosition(Key.Down, right);

            return (map[right] == WALL || map[right] == BOX) &&
                   (map[bottom] == WALL || map[bottom] == BOX) &&
                   (map[rightBottom] == WALL || map[rightBottom] == BOX);
        }

        public SokobanPathItem GetPathItem(Position from, Position to)
        {
            var pathItem = new SokobanPathItem();
            pathItem.Position = Convert(from);

            var dx = from.X - to.X;
            var dy = from.Y - to.Y;

            if (dx == 0)
            {
                pathItem.StepsToTarget = Math.Abs(dy);
                pathItem.Key = (dy < 0) ? Key.Down : Key.Up;
            }
            else
            {
                pathItem.StepsToTarget = Math.Abs(dx);
                pathItem.Key = (dx < 0) ? Key.Right : Key.Left;
            }

            return pathItem;
        }
    }
}
