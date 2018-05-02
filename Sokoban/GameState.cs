using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Sokoban
{
    public class GameState
    {
        private static readonly Key[] SupportedKeys = new Key[] { Key.Left, Key.Up, Key.Right, Key.Down };

        private const char EMPTY = '0';
        private const char KEEPER = '1';
        private const char BOX = '2';
        private const char WALL = '3';
        private const char LOCATION = '4';
        private const char BOX_ON_LOCATION = '5';
        private const char KEEPER_ON_LOCATION = '6';

        private readonly string gameState;
        private int keeperPos;

        public GameState(int width, int height, string gameState, List<Tuple<int, Key, int>> movesFromBegin, int keeperPosition = -1)
        {
            Width = width;
            Height = height;

            this.gameState = gameState;
            MovesFromBegin = movesFromBegin;

            keeperPos = keeperPosition;
            EnsureKeeperPosition();
        }

        public int Width { get; }

        public int Height { get; }

        public List<Tuple<int, Key, int>> MovesFromBegin { get; }

        public override int GetHashCode()
        {
            return gameState.GetHashCode();
        }

        public bool IsComplete()
        {
            var anyBoxOrLocation = gameState.Any(x => x == BOX || x == LOCATION);

            return !anyBoxOrLocation;
        }

        public IEnumerable<Tuple<int, Key, int>> GetPossibleMoves()
        {
            var possibleMoves = new List<Tuple<int, Key, int>>();
            var testedCells = new List<int>();
            var notTestedCells = new Dictionary<int, List<Key>>();
            notTestedCells.Add(keeperPos, new List<Key>());

            while (notTestedCells.Count > 0)
            {
                var node = notTestedCells.First();
                var currentPosition = node.Key;
                var currentPositionPath = node.Value;
                notTestedCells.Remove(currentPosition);

                testedCells.Add(currentPosition);

                List<Tuple<Key, int>> emptyNeighbors, boxNeighbors;
                AnalyzeNeighbors(currentPosition, testedCells, out emptyNeighbors, out boxNeighbors);

                foreach (var pair in boxNeighbors)
                {
                    var key = pair.Item1;
                    var boxPosition = pair.Item2;

                    var localPossibleMoves = new List<Tuple<int, Key, int>>();
                    int newBoxPosition;
                    while (CanMoveBox(key, boxPosition, out newBoxPosition))
                    {
                        var move = new Tuple<int, Key, int>(currentPosition, key, newBoxPosition);

                        if (gameState[newBoxPosition] == LOCATION || gameState[newBoxPosition] == KEEPER_ON_LOCATION)
                            localPossibleMoves.Insert(0, move);
                        else
                            localPossibleMoves.Add(move);

                        boxPosition = newBoxPosition;
                    }

                    possibleMoves.AddRange(localPossibleMoves);
                }

                foreach (var pair in emptyNeighbors)
                {
                    var positionAtKet = pair.Item2;

                    if (!notTestedCells.ContainsKey(positionAtKet))
                    {
                        var key = pair.Item1;
                        var localPath = currentPositionPath.ToList();
                        localPath.Add(key);

                        notTestedCells.Add(positionAtKet, localPath);
                    }
                }
            }

            return possibleMoves;
        }

        private void EnsureKeeperPosition()
        {
            if (keeperPos >= 0)
            {
                return;
            }

            keeperPos = gameState.IndexOf(KEEPER);
            if (keeperPos == -1)
            {
                keeperPos = gameState.IndexOf(KEEPER_ON_LOCATION);

                if (keeperPos == -1)
                {
                    throw new Exception("Keeper not found");
                }
            }
        }

        private int GetPosition(Key key, int currentPos)
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
                return (currentPos + Width >= gameState.Length) ? -1 : currentPos + Width;
            }

            return -1;
        }

        private void AnalyzeNeighbors(int currentPosition, List<int> testedPositions, out List<Tuple<Key, int>> emptyNeighbors, out List<Tuple<Key, int>> boxNeighbors)
        {
            emptyNeighbors = new List<Tuple<Key, int>>();
            boxNeighbors = new List<Tuple<Key, int>>();

            foreach (Key key in SupportedKeys)
            {
                int positionAtKey = GetPosition(key, currentPosition);

                if (positionAtKey == -1 || testedPositions.Contains(positionAtKey))
                {
                    continue;
                }

                var cellTypeAtOffset = gameState[positionAtKey];

                if (cellTypeAtOffset == EMPTY || cellTypeAtOffset == LOCATION)
                {
                    emptyNeighbors.Add(new Tuple<Key, int>(key, positionAtKey));
                }
                else if (cellTypeAtOffset == BOX || cellTypeAtOffset == BOX_ON_LOCATION)
                {
                    boxNeighbors.Add(new Tuple<Key, int>(key, positionAtKey));
                }
            }
        }

        private bool CanMoveBox(Key key, int boxPosition, out int newBoxPosition)
        {
            newBoxPosition = GetPosition(key, boxPosition);
            if (newBoxPosition == -1)
            {
                return false;
            }

            var cellTypeAtNewPosition = gameState[newBoxPosition];
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

            return (gameState[left] == WALL || gameState[left] == BOX) &&
                   (gameState[top] == WALL || gameState[top] == BOX) &&
                   (gameState[leftTop] == WALL || gameState[leftTop] == BOX);
        }

        private bool IsRightTopCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var top = GetPosition(Key.Up, position);
            var rightTop = GetPosition(Key.Up, right);

            return (gameState[right] == WALL || gameState[right] == BOX) &&
                   (gameState[top] == WALL || gameState[top] == BOX) &&
                   (gameState[rightTop] == WALL || gameState[rightTop] == BOX);
        }

        private bool IsLeftBottomCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var bottom = GetPosition(Key.Down, position);
            var leftBottom = GetPosition(Key.Down, left);

            return (gameState[left] == WALL || gameState[left] == BOX) &&
                   (gameState[bottom] == WALL || gameState[bottom] == BOX) &&
                   (gameState[leftBottom] == WALL || gameState[leftBottom] == BOX);
        }

        private bool IsRightBottomCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var bottom = GetPosition(Key.Down, position);
            var rightBottom = GetPosition(Key.Down, right);

            return (gameState[right] == WALL || gameState[right] == BOX) &&
                   (gameState[bottom] == WALL || gameState[bottom] == BOX) &&
                   (gameState[rightBottom] == WALL || gameState[rightBottom] == BOX);
        }

        public GameState MakeMove(Tuple<int, Key, int> move)
        {
            var currentGameSate = new StringBuilder(gameState);
            var currentKeeperPos = keeperPos;

            int targetKeeperPos = move.Item1, targetBoxPos = -1;
            while (targetBoxPos != move.Item3)
            {
                currentGameSate[currentKeeperPos] = (currentGameSate[currentKeeperPos] == KEEPER_ON_LOCATION) ? LOCATION : EMPTY;

                targetKeeperPos = GetPosition(move.Item2, targetKeeperPos);
                targetBoxPos = GetPosition(move.Item2, targetKeeperPos);

                currentGameSate[targetKeeperPos] = (currentGameSate[targetKeeperPos] == BOX) ? KEEPER : KEEPER_ON_LOCATION;
                currentGameSate[targetBoxPos] = (currentGameSate[targetBoxPos] == EMPTY) ? BOX : BOX_ON_LOCATION;

                currentKeeperPos = targetKeeperPos;
            }

            var moves = MovesFromBegin.ToList();
            moves.Add(move);

            return new GameState(Width, Height, currentGameSate.ToString(), moves, currentKeeperPos);
        }
    }
}
