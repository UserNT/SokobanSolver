using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Sokoban.Solver
{
    public class Navigator : INavigator
    {
        private readonly string map;

        private Navigator(int width, int height, string map)
        {
            this.Width = width;
            this.Height = height;
            this.map = map;
        }

        public int Width { get; }

        public int Height { get; }

        public char this[int? pos]
        {
            get { return pos.HasValue ? map[pos.Value] : char.MaxValue; }
        }

        public char this[int pos]
        {
            get { return map[pos]; }
        }

        public override int GetHashCode()
        {
            return map.GetHashCode();
        }

        #region Get Position

        public int? GetPosition(int column, int row)
        {
            var pos = row * Width + column;

            return ValidatePos(pos);
        }

        public int? GetPosition(Key key, int? currentPos)
        {
            return currentPos.HasValue ? GetPosition(key, currentPos.Value) : currentPos;
        }

        public int? GetPosition(Key key, int currentPos)
        {
            if (key == Key.Left)
            {
                return (currentPos % Width == 0) ? default(int?) : ValidatePos(currentPos - 1);
            }
            else if (key == Key.Right)
            {
                return ((currentPos + 1) % Width == 0) ? default(int?) : ValidatePos(currentPos + 1);
            }
            else if (key == Key.Up)
            {
                return (currentPos - Width < 0) ? default(int?) : ValidatePos(currentPos - Width);
            }
            else if (key == Key.Down)
            {
                return (currentPos + Width >= map.Length) ? default(int?) : ValidatePos(currentPos + Width);
            }

            return default(int?);
        }

        public int GetKeeperPosition()
        {
            return map.IndexOfAny(new[] { Sokoban.KEEPER, Sokoban.KEEPER_ON_LOCATION });
        }

        private int? ValidatePos(int pos)
        {
            return (pos >= 0 && pos < map.Length) ? pos : default(int?);
        }

        public Key GetOppositeKey(Key key)
        {
            return key == Key.Up ? Key.Down :
                   key == Key.Down ? Key.Up :
                   key == Key.Left ? Key.Right :
                   Key.Left;
        }

        public double GetDistance(int pos1, int pos2)
        {
            var p1 = Convert(pos1);
            var p2 = Convert(pos2);

            double deltaX = p1.X - p2.X;
            double deltaY = p1.Y - p2.Y;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public Point Convert(int pos)
        {
            var y = pos / Width;
            var x = pos - y * Width;

            return new Point(x, y);
        }

        #endregion

        #region Push and Drag

        public bool CanPush(Key key, int? keeperPos = null)
        {
            if (!keeperPos.HasValue)
            {
                keeperPos = GetKeeperPosition();
            }

            var boxPos = GetPosition(key, keeperPos.Value);

            if (boxPos.HasValue &&
                (this[boxPos.Value] == Sokoban.BOX || this[boxPos.Value] == Sokoban.BOX_ON_LOCATION))
            {
                int? tmp;
                return CanMoveBox(key, boxPos.Value, out tmp);
            }

            return false;
        }

        public INavigator Push(Key key, int? keeperPos = null)
        {
            if (!keeperPos.HasValue)
            {
                keeperPos = GetKeeperPosition();
            }

            var boxPos = GetPosition(key, keeperPos);
            var boxTarget = GetPosition(key, boxPos);

            var newMap = map.Replace(Sokoban.KEEPER, Sokoban.EMPTY)
                            .Replace(Sokoban.KEEPER_ON_LOCATION, Sokoban.LOCATION);

            var sb = new StringBuilder(newMap);

            sb[boxTarget.Value] = (sb[boxTarget.Value] == Sokoban.LOCATION) ? Sokoban.BOX_ON_LOCATION : Sokoban.BOX;
            sb[boxPos.Value] = (sb[boxPos.Value] == Sokoban.BOX_ON_LOCATION) ? Sokoban.KEEPER_ON_LOCATION : Sokoban.KEEPER;
            sb[keeperPos.Value] = (sb[keeperPos.Value] == Sokoban.KEEPER_ON_LOCATION) ? Sokoban.LOCATION : Sokoban.EMPTY;

            return new Navigator(Width, Height, sb.ToString());
        }

        public bool CanDrag(Key key, int? keeperPos = null)
        {
            if (!keeperPos.HasValue)
            {
                keeperPos = GetKeeperPosition();
            }

            var newKeeperPos = GetPosition(key, keeperPos.Value);
            if (newKeeperPos.HasValue &&
                (this[newKeeperPos.Value] == Sokoban.EMPTY || this[newKeeperPos.Value] == Sokoban.LOCATION))
            {
                var boxPos = GetPosition(GetOppositeKey(key), keeperPos.Value);
                if (boxPos.HasValue &&
                    (this[boxPos.Value] == Sokoban.BOX || this[boxPos.Value] == Sokoban.BOX_ON_LOCATION))
                {
                    return true;
                }
            }

            return false;
        }

        public INavigator Drag(Key key, int? keeperPos = null)
        {
            if (!keeperPos.HasValue)
            {
                keeperPos = GetKeeperPosition();
            }

            var newMap = map.Replace(Sokoban.KEEPER, Sokoban.EMPTY)
                            .Replace(Sokoban.KEEPER_ON_LOCATION, Sokoban.LOCATION);

            var sb = new StringBuilder(newMap);

            var keeperTarget = GetPosition(key, keeperPos.Value);
            var boxPos = GetPosition(GetOppositeKey(key), keeperPos.Value);

            sb[boxPos.Value] = (sb[boxPos.Value] == Sokoban.BOX_ON_LOCATION) ? Sokoban.LOCATION : Sokoban.EMPTY;
            sb[keeperPos.Value] = (sb[keeperPos.Value] == Sokoban.LOCATION) ? Sokoban.BOX_ON_LOCATION : Sokoban.BOX;
            sb[keeperTarget.Value] = (sb[keeperTarget.Value] == Sokoban.LOCATION) ? Sokoban.KEEPER_ON_LOCATION : Sokoban.KEEPER;

            return new Navigator(Width, Height, sb.ToString());
        }

        public bool CanHoldKeeper(int? pos)
        {
            if (!pos.HasValue)
            {
                return false;
            }

            return this[pos] == Sokoban.EMPTY ||
                   this[pos] == Sokoban.LOCATION ||
                   this[pos] == Sokoban.KEEPER ||
                   this[pos] == Sokoban.KEEPER_ON_LOCATION;
        }

        private bool CanDragRecursively(int keeperStartPos, int boxStartPos, int keeperEndPos, int boxEndPos, HashSet<INavigator> drags)
        {
            if (!drags.Add(this))
            {
                return false;
            }

            var touchPoints = new List<SokobanPathItem>();

            ForeachNeighborsRecursively(keeperStartPos, (currentPos, key, neighbor, cellType) =>
            {
                if (neighbor == boxStartPos)
                {
                    var targetKeeperPos = GetPosition(GetOppositeKey(key), currentPos);

                    if (CanHoldKeeper(targetKeeperPos))
                    {
                        touchPoints.Add(new SokobanPathItem()
                        {
                            Position = currentPos,
                            Key = key,
                            StepsToTarget = 1
                        });
                    }
                }

                return CanHoldKeeper(neighbor);
            });

            if (touchPoints.Count == 0)
            {
                // keeper doesn`t even touch the box
                return false;
            }

            foreach (var touchPoint in touchPoints)
            {
                var keeperPos = touchPoint.Position;
                var keeperTarget = GetPosition(GetOppositeKey(touchPoint.Key), keeperPos);
                var boxPos = GetPosition(touchPoint.Key, keeperPos);

                if (keeperTarget == keeperEndPos && keeperPos == boxEndPos)
                {
                    return true;
                }
                else
                {
                    var newNavigator = (Navigator)Drag(keeperPos, keeperTarget.Value, boxPos.Value);

                    if (newNavigator.CanDragRecursively(keeperTarget.Value, keeperPos, keeperEndPos, boxEndPos, drags))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanDragContinuosly(INavigator navigator, int keeperStartPos, int boxStartPos, int keeperEndPos, int boxEndPos, Queue<Tuple<INavigator, int, int, int, int>> scope, HashSet<INavigator> drags)
        {
            if (!drags.Add(navigator))
            {
                return false;
            }

            var touchPoints = new List<SokobanPathItem>();

            navigator.ForeachNeighborsRecursively(keeperStartPos, (currentPos, key, neighbor, cellType) =>
            {
                if (neighbor == boxStartPos)
                {
                    var targetKeeperPos = navigator.GetPosition(navigator.GetOppositeKey(key), currentPos);

                    if (navigator.CanHoldKeeper(targetKeeperPos))
                    {
                        touchPoints.Add(new SokobanPathItem()
                        {
                            Position = currentPos,
                            Key = key,
                            StepsToTarget = 1
                        });
                    }
                }

                return navigator.CanHoldKeeper(neighbor);
            });

            if (touchPoints.Count == 0)
            {
                // keeper doesn`t even touch the box
                return false;
            }

            foreach (var touchPoint in touchPoints)
            {
                var keeperPos = touchPoint.Position;
                var keeperTarget = navigator.GetPosition(navigator.GetOppositeKey(touchPoint.Key), keeperPos);
                var boxPos = navigator.GetPosition(touchPoint.Key, keeperPos);

                if (keeperTarget == keeperEndPos && keeperPos == boxEndPos)
                {
                    return true;
                }
                else
                {
                    var newNavigator = (Navigator)Drag(keeperPos, keeperTarget.Value, boxPos.Value);

                    scope.Enqueue(new Tuple<INavigator, int, int, int, int>(newNavigator, keeperTarget.Value, keeperPos, keeperEndPos, boxEndPos));
                }
            }

            return false;
        }

        public bool CanDrag(int keeperStartPos, int boxStartPos, int keeperEndPos, int boxEndPos)
        {
            var drags = new HashSet<INavigator>();
            var scope = new Queue<Tuple<INavigator, int, int, int, int>>();
            scope.Enqueue(new Tuple<INavigator, int, int, int, int>(this, keeperStartPos, boxStartPos, keeperEndPos, boxEndPos));

            while (scope.Count > 0)
            {
                var currentNode = scope.Dequeue();

                if (CanDragContinuosly(currentNode.Item1, currentNode.Item2, currentNode.Item3, currentNode.Item4, currentNode.Item5, scope, drags))
                {
                    return true;
                }
            }

            return false;
        }

        public INavigator Drag(int keeperPos, int keeperTarget, int boxPos)
        {
            var newMap = map.Replace(Sokoban.KEEPER, Sokoban.EMPTY)
                            .Replace(Sokoban.KEEPER_ON_LOCATION, Sokoban.LOCATION);

            var sb = new StringBuilder(newMap);

            sb[boxPos] = (sb[boxPos] == Sokoban.BOX_ON_LOCATION) ? Sokoban.LOCATION : Sokoban.EMPTY;
            sb[keeperPos] = (sb[keeperPos] == Sokoban.LOCATION) ? Sokoban.BOX_ON_LOCATION : Sokoban.BOX;
            sb[keeperTarget] = (sb[keeperTarget] == Sokoban.LOCATION) ? Sokoban.KEEPER_ON_LOCATION : Sokoban.KEEPER;

            return new Navigator(Width, Height, sb.ToString());
        }

        #endregion

        #region CanMoveBox

        public bool CanMoveBox(Key key, int boxPosition, out int? newBoxPosition)
        {
            newBoxPosition = GetPosition(key, boxPosition);
            if (!newBoxPosition.HasValue)
            {
                return false;
            }

            var cellTypeAtNewPosition = this[newBoxPosition];
            if (cellTypeAtNewPosition == Sokoban.BOX ||
                cellTypeAtNewPosition == Sokoban.BOX_ON_LOCATION ||
                cellTypeAtNewPosition == Sokoban.WALL)
            {
                return false;
            }
            else if (cellTypeAtNewPosition == Sokoban.LOCATION)
            {
                return true;
            }

            if (key == Key.Left)
            {
                if (IsLeftTopCorner(newBoxPosition.Value) || IsLeftBottomCorner(newBoxPosition.Value))
                {
                    return false;
                }
            }
            else if (key == Key.Up)
            {
                if (IsLeftTopCorner(newBoxPosition.Value) || IsRightTopCorner(newBoxPosition.Value))
                {
                    return false;
                }
            }
            else if (key == Key.Right)
            {
                if (IsRightTopCorner(newBoxPosition.Value) || IsRightBottomCorner(newBoxPosition.Value))
                {
                    return false;
                }
            }
            else if (key == Key.Down)
            {
                if (IsLeftBottomCorner(newBoxPosition.Value) || IsRightBottomCorner(newBoxPosition.Value))
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

            return (this[left] == Sokoban.WALL || this[left] == Sokoban.BOX) &&
                   (this[top] == Sokoban.WALL || this[top] == Sokoban.BOX) &&
                   (this[leftTop] == Sokoban.WALL || this[leftTop] == Sokoban.BOX);
        }

        private bool IsRightTopCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var top = GetPosition(Key.Up, position);
            var rightTop = GetPosition(Key.Up, right);

            return (this[right] == Sokoban.WALL || this[right] == Sokoban.BOX) &&
                   (this[top] == Sokoban.WALL || this[top] == Sokoban.BOX) &&
                   (this[rightTop] == Sokoban.WALL || this[rightTop] == Sokoban.BOX);
        }

        private bool IsLeftBottomCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var bottom = GetPosition(Key.Down, position);
            var leftBottom = GetPosition(Key.Down, left);

            return (this[left] == Sokoban.WALL || this[left] == Sokoban.BOX) &&
                   (this[bottom] == Sokoban.WALL || this[bottom] == Sokoban.BOX) &&
                   (this[leftBottom] == Sokoban.WALL || this[leftBottom] == Sokoban.BOX);
        }

        private bool IsRightBottomCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var bottom = GetPosition(Key.Down, position);
            var rightBottom = GetPosition(Key.Down, right);

            return (this[right] == Sokoban.WALL || this[right] == Sokoban.BOX) &&
                   (this[bottom] == Sokoban.WALL || this[bottom] == Sokoban.BOX) &&
                   (this[rightBottom] == Sokoban.WALL || this[rightBottom] == Sokoban.BOX);
        }

        #endregion

        #region foreach

        public void Foreach(char[] cellTypes, Action<int> callback)
        {
            int currentIndex = -1;

            do
            {
                currentIndex = map.IndexOfAny(cellTypes, currentIndex + 1);

                if (currentIndex != -1)
                {
                    callback(currentIndex);
                }
            }
            while (currentIndex != -1);
        }

        public void Foreach(char[] cellTypes, Action<int, int> callback)
        {
            var scope = new Queue<int>();
            var visitedCells = new BitArray(map.Length);

            int areaId = -1;
            int currentIndex = -1;
            currentIndex = map.IndexOfAny(cellTypes, currentIndex + 1);

            while (currentIndex != -1)
            {
                if (!visitedCells.Get(currentIndex))
                {
                    areaId++;
                    scope.Enqueue(currentIndex);
                    visitedCells.Set(currentIndex, true);
                }

                while (scope.Count > 0)
                {
                    var currentPosition = scope.Dequeue();

                    callback(areaId, currentPosition);

                    foreach (var key in Sokoban.SupportedKeys)
                    {
                        var neighbor = GetPosition(key, currentPosition);

                        if (neighbor.HasValue && !visitedCells.Get(neighbor.Value) &&
                            cellTypes.Contains(this[neighbor.Value]))
                        {
                            scope.Enqueue(neighbor.Value);
                            visitedCells.Set(neighbor.Value, true);
                        }
                    }
                }

                currentIndex = map.IndexOfAny(cellTypes, currentIndex + 1);
            }
        }

        public void ForeachNeighbors(int position, Action<int?, Key, char> callback)
        {
            foreach (var key in Sokoban.SupportedKeys)
            {
                var neighbor = GetPosition(key, position);
                var cellType = neighbor.HasValue ? this[neighbor.Value] : '0';

                callback(neighbor, key, cellType);
            }
        }

        public void ForeachNeighborsRecursively(int position, Func<int, Key, int, char, bool> canContinue)
        {
            var visited = new BitArray(Width * Height);

            var scope = new Queue<int>();
            scope.Enqueue(position);
            visited.Set(position, true);

            while (scope.Count > 0)
            {
                var pos = scope.Dequeue();
                visited.Set(pos, true);

                foreach (var key in Sokoban.SupportedKeys)
                {
                    var neighbor = GetPosition(key, pos);
                    if (neighbor.HasValue && !visited.Get(neighbor.Value))
                    {
                        //visited.Set(neighbor.Value, true);

                        if (canContinue(pos, key, neighbor.Value, this[neighbor.Value]))
                        {
                            scope.Enqueue(neighbor.Value);
                        }
                    }
                }
            }
        }

        #endregion

        #region Using INavigator

        public static INavigator Using(string gameData)
        {
            if (gameData.Contains('{'))
            {
                var responseData = JsonConvert.DeserializeObject<Response>(gameData);

                gameData = responseData.Data.GameData;
            }

            var parts = gameData.Split('*');

            return Using(int.Parse(parts[1]), int.Parse(parts[0]), parts[2]);
        }

        public static INavigator Using(int width, int height, string map)
        {
            return new Navigator(width, height, map);
        }

        public INavigator ReplaceWithBoxes(LocationGroup group)
        {
            var newMap = map.Replace(Sokoban.BOX, Sokoban.EMPTY)
                            .Replace(Sokoban.BOX_ON_LOCATION, Sokoban.LOCATION)
                            .Replace(Sokoban.KEEPER, Sokoban.EMPTY)
                            .Replace(Sokoban.KEEPER_ON_LOCATION, Sokoban.LOCATION);

            var sb = new StringBuilder(newMap);

            foreach (var location in group.Positions)
            {
                sb[location] = Sokoban.BOX_ON_LOCATION;
            }

            sb[group.EntryPoints[0].Position] = Sokoban.KEEPER;

            return new Navigator(Width, Height, sb.ToString());
        }

        public INavigator Replace(IEnumerable<int> positions, char cellType)
        {
            var sb = new StringBuilder(map);

            foreach (var position in positions)
            {
                sb[position] = cellType;
            }

            return new Navigator(Width, Height, sb.ToString());
        }

        #endregion
    }
}
