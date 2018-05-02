using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Sokoban.Solver
{
    public class ColorMap
    {
        private const int InitialColorIndex = 10;
        public const int LocationColorIndex = 9;
        public const int KeeperColorIndex = 8;
        public const int KeeperOnLocationColorIndex = 7;

        public static readonly Key[] SupportedKeys = new Key[] { Key.Left, Key.Up, Key.Right, Key.Down };

        public const char EMPTY = '0';
        public const char KEEPER = '1';
        public const char BOX = '2';
        public const char WALL = '3';
        public const char LOCATION = '4';
        public const char BOX_ON_LOCATION = '5';
        public const char KEEPER_ON_LOCATION = '6';

        private StringBuilder colorMap;

        public ColorMap(int width, int height, string map)
        {
            Width = width;
            Height = height;

            colorMap = new StringBuilder(map);

            ColorToBoxTouchPoints = new Dictionary<char, List<SokobanPathItem>>();
            ColorToLocationTouchPoints = new Dictionary<char, List<SokobanPathItem>>();
            ColorToKeeperTouchPoint = new Dictionary<char, List<SokobanPathItem>>();

            FillAreas();
        }

        public int Width { get; }

        public int Height { get; }

        public override string ToString()
        {
            return colorMap.ToString();
        }

        public Dictionary<char, List<SokobanPathItem>> ColorToBoxTouchPoints { get; private set; }

        public Dictionary<char, List<SokobanPathItem>> ColorToLocationTouchPoints { get; private set; }

        public Dictionary<char, List<SokobanPathItem>> ColorToKeeperTouchPoint { get; private set; }

        private Dictionary<SokobanPathItem, List<int>> locationFillingOrder;
        public Dictionary<SokobanPathItem, List<int>> LocationFillingOrder
        {
            get
            {
                if (locationFillingOrder == null)
                {
                    locationFillingOrder = GetTouchPointToLocationFillingOrder();
                }

                return locationFillingOrder;
            }
            private set
            {
                locationFillingOrder = value;
            }
        }

        public bool IsComplete()
        {
            var anyBoxOrLocation = colorMap.ToString().Any(x => x == BOX || x == LocationColorIndex);

            return !anyBoxOrLocation;
        }

        private Dictionary<SokobanPathItem, List<int>> GetTouchPointToLocationFillingOrder()
        {
            var result = new Dictionary<SokobanPathItem, List<int>>();

            foreach (var touchPoint in ColorToLocationTouchPoints.Values.SelectMany(x => x))
            {
                var estimations = new List<Tuple<int, double>>();

                var lastLocationPosition = GetPosition(touchPoint.Key, touchPoint.Position);
                var lastLocationY = lastLocationPosition / Width;
                var lastLocationX = lastLocationPosition - lastLocationY * Width;

                var scope = new Queue<int>();
                scope.Enqueue(touchPoint.Position);

                var visitedPositions = new HashSet<int>();

                while (scope.Count > 0)
                {
                    var currentPosition = scope.Dequeue();

                    OnNextPosition(currentPosition, (key, nextPosition) =>
                    {
                        if (colorMap[nextPosition] == LOCATION || colorMap[nextPosition] == LocationColorIndex ||
                            colorMap[nextPosition] == KEEPER_ON_LOCATION || colorMap[nextPosition] == KeeperOnLocationColorIndex ||
                            colorMap[nextPosition] == BOX_ON_LOCATION)
                        {
                            var oppositeKey = key == Key.Up ? Key.Down :
                                              key == Key.Down ? Key.Up :
                                              key == Key.Left ? Key.Right :
                                              Key.Left;

                            var oppositeNeighbor = GetPosition(oppositeKey, currentPosition);

                            if (colorMap[oppositeNeighbor] != SokobanMap.WALL)
                            {
                                if (visitedPositions.Add(nextPosition))
                                {
                                    scope.Enqueue(nextPosition);

                                    var locationY = nextPosition / Width;
                                    var locationX = nextPosition - locationY * Width;

                                    var dx = lastLocationX - locationX;
                                    var dy = lastLocationY - locationY;

                                    estimations.Add(new Tuple<int, double>(nextPosition, Math.Sqrt(dx * dx + dy * dy)));
                                }
                            }
                        }
                    });
                }

                estimations.Sort((node1, node2) => node2.Item2.CompareTo(node1.Item2));

                result.Add(touchPoint, new List<int>(estimations.Select(pair => pair.Item1)));
            }

            return result;
        }

        #region Get Position

        public int GetPosition(int column, int row)
        {
            return row * Width + column;
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
                return (currentPos + Width >= colorMap.Length) ? -1 : currentPos + Width;
            }

            return -1;
        }

        #endregion

        #region Move Keeper

        public bool TryMoveKeeper(Key key, out ColorMap newColorMap)
        {
            newColorMap = this;

            if (SupportedKeys.Contains(key))
            {
                if (CanMoveKeeper(key))
                {
                    newColorMap = MoveKeeper(key);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public ColorMap MoveKeeper(SokobanPathItem touchPoint)
        {
            var keeperPos = GetKeeperPos();
            var targetKeeperPos = GetPosition(touchPoint.Key, touchPoint.Position);

            return MoveKeeper(touchPoint.Key, keeperPos, targetKeeperPos);
        }

        private bool CanMoveKeeper(Key key)
        {
            var firstTouchColor = ColorToKeeperTouchPoint.First().Key;
            var firstTouchPoint = ColorToKeeperTouchPoint[firstTouchColor].First();

            var keeperPos = GetPosition(firstTouchPoint.Key, firstTouchPoint.Position);
            var targetKeeperPos = GetPosition(key, keeperPos);

            if (ColorToKeeperTouchPoint.ContainsKey(colorMap[targetKeeperPos]) || colorMap[targetKeeperPos] == LocationColorIndex)
            {
                return true;
            }
            else if (colorMap[targetKeeperPos] == BOX ||
                     colorMap[targetKeeperPos] == BOX_ON_LOCATION)
            {
                int tmp;
                return CanMoveBox(key, targetKeeperPos, out tmp);
            }

            return false;
        }

        private ColorMap MoveKeeper(Key key)
        {
            int keeperPos = GetKeeperPos();
            var targetKeeperPos = GetPosition(key, keeperPos);

            return MoveKeeper(key, keeperPos, targetKeeperPos);
        }

        private int GetKeeperPos()
        {
            var firstTouchColor = ColorToKeeperTouchPoint.First().Key;
            var firstTouchPoint = ColorToKeeperTouchPoint[firstTouchColor].First();

            return GetPosition(firstTouchPoint.Key, firstTouchPoint.Position);
        }

        private ColorMap MoveKeeper(Key key, int keeperPos, int targetKeeperPos)
        {
            var newMap = RecoverColors();
            newMap[keeperPos] = newMap[keeperPos] == KEEPER ? EMPTY : LOCATION;

            if (newMap[targetKeeperPos] == EMPTY || newMap[targetKeeperPos] == LOCATION)
            {
                newMap[targetKeeperPos] = newMap[targetKeeperPos] == EMPTY ? KEEPER : KEEPER_ON_LOCATION;

                return new ColorMap(Width, Height, newMap.ToString())
                {
                    LocationFillingOrder = this.LocationFillingOrder
                };
            }
            else if (colorMap[targetKeeperPos] == BOX || colorMap[targetKeeperPos] == BOX_ON_LOCATION)
            {
                var targetBoxPos = GetPosition(key, targetKeeperPos);
                newMap[targetBoxPos] = newMap[targetBoxPos] == EMPTY ? BOX : BOX_ON_LOCATION;
                newMap[targetKeeperPos] = newMap[targetKeeperPos] == BOX ? KEEPER : KEEPER_ON_LOCATION;

                return new ColorMap(Width, Height, newMap.ToString())
                {
                    LocationFillingOrder = this.LocationFillingOrder
                };
            }

            return this;
        }

        private StringBuilder RecoverColors()
        {
            var map = new StringBuilder(colorMap.ToString());

            for (int i = 0; i < map.Length; i++)
            {
                if (map[i] == KeeperColorIndex)
                {
                    map[i] = KEEPER;
                }
                else if (map[i] == KeeperOnLocationColorIndex)
                {
                    map[i] = KEEPER_ON_LOCATION;
                }
                else if (map[i] == LocationColorIndex)
                {
                    map[i] = LOCATION;
                }
                else if (map[i] < EMPTY)
                {
                    map[i] = EMPTY;
                }
            }

            return map;
        }

        #endregion

        #region CanMoveBox

        public bool CanMoveBox(Key key, int boxPosition, out int newBoxPosition)
        {
            newBoxPosition = GetPosition(key, boxPosition);
            if (newBoxPosition == -1)
            {
                return false;
            }

            var cellTypeAtNewPosition = colorMap[newBoxPosition];
            if (cellTypeAtNewPosition == BOX ||
                cellTypeAtNewPosition == BOX_ON_LOCATION ||
                cellTypeAtNewPosition == WALL)
            {
                return false;
            }
            else if (cellTypeAtNewPosition == LocationColorIndex)
            {
                return true;
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

            return (colorMap[left] == WALL || colorMap[left] == BOX) &&
                   (colorMap[top] == WALL || colorMap[top] == BOX) &&
                   (colorMap[leftTop] == WALL || colorMap[leftTop] == BOX);
        }

        private bool IsRightTopCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var top = GetPosition(Key.Up, position);
            var rightTop = GetPosition(Key.Up, right);

            return (colorMap[right] == WALL || colorMap[right] == BOX) &&
                   (colorMap[top] == WALL || colorMap[top] == BOX) &&
                   (colorMap[rightTop] == WALL || colorMap[rightTop] == BOX);
        }

        private bool IsLeftBottomCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var bottom = GetPosition(Key.Down, position);
            var leftBottom = GetPosition(Key.Down, left);

            return (colorMap[left] == WALL || colorMap[left] == BOX) &&
                   (colorMap[bottom] == WALL || colorMap[bottom] == BOX) &&
                   (colorMap[leftBottom] == WALL || colorMap[leftBottom] == BOX);
        }

        private bool IsRightBottomCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var bottom = GetPosition(Key.Down, position);
            var rightBottom = GetPosition(Key.Down, right);

            return (colorMap[right] == WALL || colorMap[right] == BOX) &&
                   (colorMap[bottom] == WALL || colorMap[bottom] == BOX) &&
                   (colorMap[rightBottom] == WALL || colorMap[rightBottom] == BOX);
        }

        #endregion

        #region Fill

        private void FillAreas()
        {
            char currentColor = Convert.ToChar(InitialColorIndex);

            int emptyPosition;
            while (OnNextWalkableArea(out emptyPosition))
            {
                var scope = new Queue<Tuple<int, Key?>>();
                scope.Enqueue(new Tuple<int, Key?>(emptyPosition, null));

                while (scope.Count > 0)
                {
                    var currentNode = scope.Dequeue();
                    int currentPosition = currentNode.Item1;

                    if (colorMap[currentPosition] == currentColor)
                    {
                        // already processed
                        continue;
                    }
                    
                    if (colorMap[currentPosition] == EMPTY)
                    {
                        colorMap[currentPosition] = currentColor;
                    }
                    else if (colorMap[currentPosition] == LOCATION)
                    {
                        colorMap[currentPosition] = Convert.ToChar(LocationColorIndex);
                    }
                    else if (colorMap[currentPosition] == KEEPER)
                    {
                        colorMap[currentPosition] = Convert.ToChar(KeeperColorIndex);
                    }
                    else if (colorMap[currentPosition] == KEEPER_ON_LOCATION)
                    {
                        colorMap[currentPosition] = Convert.ToChar(KeeperOnLocationColorIndex);
                    }

                    OnNextPosition(currentNode.Item1, (key, nextPosition) =>
                    {
                        if (colorMap[nextPosition] == EMPTY)
                        {
                            scope.Enqueue(new Tuple<int, Key?>(nextPosition, key));
                        }
                        else if (colorMap[nextPosition] == LOCATION)
                        {
                            scope.Enqueue(new Tuple<int, Key?>(nextPosition, key));

                            PopulateColorToLocationTouchPoints(key, nextPosition, currentColor, currentPosition);
                        }
                        else if (colorMap[nextPosition] == LocationColorIndex)
                        {
                            PopulateColorToLocationTouchPoints(key, nextPosition, currentColor, currentPosition);
                        }
                        else if (colorMap[nextPosition] == KEEPER || colorMap[nextPosition] == KEEPER_ON_LOCATION)
                        {
                            scope.Enqueue(new Tuple<int, Key?>(nextPosition, key));

                            PopulateColorToKeeperTouchPoints(key, currentColor, currentPosition);
                        }
                        else if (colorMap[nextPosition] == KeeperColorIndex || colorMap[nextPosition] == KeeperOnLocationColorIndex)
                        {
                            PopulateColorToKeeperTouchPoints(key, currentColor, currentPosition);
                        }
                        else if (colorMap[nextPosition] == BOX ||
                                 colorMap[nextPosition] == BOX_ON_LOCATION)
                        {
                            PopulateColorToBoxTouchPoints(key, nextPosition, currentColor, currentPosition);
                        }
                    });
                }

                currentColor++;
            }
        }

        private void PopulateColorToBoxTouchPoints(Key key, int nextPosition, char currentColor, int currentPosition)
        {
            int tmp;
            if (CanMoveBox(key, nextPosition, out tmp))
            {
                if (!ColorToBoxTouchPoints.ContainsKey(currentColor))
                {
                    ColorToBoxTouchPoints.Add(currentColor, new List<SokobanPathItem>());
                }

                ColorToBoxTouchPoints[currentColor].Add(new SokobanPathItem()
                {
                    Position = currentPosition,
                    Key = key,
                    StepsToTarget = 1
                });
            }
        }

        private void PopulateColorToLocationTouchPoints(Key key, int nextPosition, char currentColor, int currentPosition)
        {
            if ((colorMap[currentPosition] == EMPTY || colorMap[currentPosition] == currentColor) &&
                (colorMap[nextPosition] == LOCATION || colorMap[nextPosition] == LocationColorIndex))
            {
                var oppositeKey = key == Key.Up ? Key.Down :
                                  key == Key.Down ? Key.Up :
                                  key == Key.Left ? Key.Right :
                                  Key.Left;

                var oppositeNeighbor = GetPosition(oppositeKey, currentPosition);

                if (colorMap[oppositeNeighbor] != SokobanMap.WALL)
                {
                    if (!ColorToLocationTouchPoints.ContainsKey(currentColor))
                    {
                        ColorToLocationTouchPoints.Add(currentColor, new List<SokobanPathItem>());
                    }

                    if (!ColorToLocationTouchPoints[currentColor].Any(x => x.Key == key && x.Position == currentPosition))
                    {
                        ColorToLocationTouchPoints[currentColor].Add(new SokobanPathItem()
                        {
                            Position = currentPosition,
                            Key = key,
                            StepsToTarget = 1
                        });
                    }
                }
            }
        }

        private void PopulateColorToKeeperTouchPoints(Key key, char currentColor, int currentPosition)
        {
            if (!ColorToKeeperTouchPoint.ContainsKey(currentColor))
            {
                ColorToKeeperTouchPoint.Add(currentColor, new List<SokobanPathItem>());
            }

            ColorToKeeperTouchPoint[currentColor].Add(new SokobanPathItem()
            {
                Position = currentPosition,
                Key = key,
                StepsToTarget = 1
            });
        }

        private void OnNextPosition(int currentPosition, Action<Key, int> callback)
        {
            foreach (var key in SupportedKeys)
            {
                var nextPosition = GetPosition(key, currentPosition);

                if (nextPosition < 0 ||
                    nextPosition >= colorMap.Length ||
                    colorMap[nextPosition] == WALL)
                {
                    continue;
                }

                callback(key, nextPosition);
            }
        }

        private bool OnNextWalkableArea(out int emptyPosition)
        {
            emptyPosition = colorMap.ToString().IndexOfAny(new[] { EMPTY, LOCATION, KEEPER, KEEPER_ON_LOCATION });

            return emptyPosition != -1;
        }

        #endregion
    }
}