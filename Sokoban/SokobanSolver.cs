using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sokoban
{
    public class SokobanSolver
    {
        private readonly string gameData;
        private readonly TaskCompletionSource<List<Tuple<int, Key, int>>> tcs = new TaskCompletionSource<List<Tuple<int, Key, int>>>();
        //private readonly BlockingCollection<GameState> statesToProcess = new BlockingCollection<GameState>();
        private readonly ConcurrentBag<GameState> statesToProcess = new ConcurrentBag<GameState>();
        private readonly ConcurrentDictionary<int, object> processedStates = new ConcurrentDictionary<int, object>();

        private readonly SokobanAI ai;

        public SokobanSolver(string gameData)
        {
            this.gameData = gameData;

            var parts = gameData.Split('*');

            var height = int.Parse(parts[0]);
            var width = int.Parse(parts[1]);

            ai = new SokobanAI(width, height, parts[2]);

            //var initialState = new GameState(width, height, parts[2], new List<Tuple<int, Key, int>>());
            //statesToProcess.Add(initialState);
        }

        public Task<List<Tuple<int, Key, int>>> Solve()
        {
            var tasks = Enumerable.Range(0, Environment.ProcessorCount - 3)
                                  .Select(x => CreateProcessTask())
                                  .ToList();

            return tcs.Task;
        }

        private Task CreateProcessTask()
        {
            return Task.Factory.StartNew(() =>
            {
                Process();
            }, TaskCreationOptions.LongRunning);
        }

        private void Process()
        {
            var scenarios = ai.FindScenatios();

            foreach (var group in scenarios)
            {
                foreach (var scenario in group)
                {
                    SokobanPath path;
                    if (ai.TryFindPathToEntryPointUsingKeeper(scenario, out path))
                    {
                        ai.Move(path);
                    }
                }
            }
        }

        //private async void Process()
        //{
        //    GameState state;
        //    while (true)
        //    {
        //        if (statesToProcess.TryTake(out state))
        //        {
        //            var hash = state.GetHashCode();

        //            if (!processedStates.TryAdd(hash, null))
        //            {
        //                // already processed, so skip this state
        //                continue;
        //            }

        //            var possibleMoves = state.GetPossibleMoves();

        //            foreach (var move in possibleMoves)
        //            {
        //                var newState = state.MakeMove(move);

        //                if (newState.IsComplete())
        //                {
        //                    tcs.SetResult(newState.MovesFromBegin);

        //                    return;
        //                }
        //                else
        //                {
        //                    statesToProcess.Add(newState);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            await Task.Delay(5);
        //        }
        //    }
        //}

        //public Task<List<Key>> Solve()
        //{
        //    var x = gameData.ToString();
        //    return Task.Factory.StartNew(() =>
        //    {
        //        //var paths = GetPathsToBoxes(gameData);
        //        //paths = FilterImpossibleMoves(paths);
        //        //var possibleMoves = new List<Move>();
        //        //Move currentMove = null;
        //        //PopulatePossibleMoves(possibleMoves, gameData.KeeperCell, gameData.Map, ref currentMove);


        //        var orderedLocations = LocationCell.GetLocationCellsToClose(gameData);
        //        foreach (var locationToClose in orderedLocations)
        //        {

        //        }

        //        var moves = new List<Key>();
        //        //var path = RepeatUntilComplete(gameData);
        //        /*var canMoveBoxes = CanMoveBox.Find(gameData);
        //        foreach (var canMoveBox in canMoveBoxes)
        //        {
        //            var gameDataAfterMove = gameData.MakeMove(canMoveBox.Path.ToArray());
        //            var canMoveBoxes2 = CanMoveBox.Find(gameDataAfterMove);

        //            foreach (var canMoveBox2 in canMoveBoxes2)
        //            {
        //                var gameDataAfterMove2 = gameData.MakeMove(canMoveBox2.Path.ToArray());
        //                var canMoveBox3 = CanMoveBox.Find(gameDataAfterMove2);
        //            }
        //        }*/

        //        return moves;
        //    });
        //}

        //private Cell[] RepeatUntilComplete(GameData gameData)
        //{
        //    var canMoveBoxes = CanMoveBox.Find(gameData);

        //    foreach (var canMoveBox in canMoveBoxes)
        //    {
        //        var path = canMoveBox.Path.ToArray();

        //        var gameDataAfterMove = gameData.MakeMove(path);

        //        if (gameDataAfterMove.IsComplete())
        //        {
        //            return path;
        //        }
        //        else
        //        {
        //            path = RepeatUntilComplete(gameDataAfterMove);

        //            if (path != null)
        //            {
        //                return path;
        //            }
        //        }
        //    }

        //    return null;
        //}

        //private List<Cell[]> FilterImpossibleMoves(List<Cell[]> paths)
        //{
        //    var validPaths = new List<Cell[]>();

        //    foreach (var path in paths)
        //    {
        //        if (CanKeeperMoveBox(path[path.Length - 2], path[path.Length - 1], gameData.Map))
        //        {
        //            var map = MakeMove(path, gameData.Map);
        //            var newGameData = new GameData(map);
        //            var paths1 = GetPathsToBoxes(newGameData);
        //            //if (!IsInvalidMap(map))
        //            //{
        //            //    validPaths.Add(path);
        //            //}
        //        }
        //    }

        //    return validPaths;
        //}

        //private bool IsInvalidMap(CellType[,] map)
        //{

        //}

        //private Cell GetTargetCell(Cell keeperCell, Cell boxCell, CellType[,] map)
        //{
        //    var xOffset = boxCell.X - keeperCell.X;
        //    var yOffset = boxCell.Y - keeperCell.Y;

        //    var targetX = boxCell.X + xOffset;
        //    var targetY = boxCell.Y + yOffset;

        //    var targetCellType = map[targetX, targetY];

        //    return new Cell(targetX, targetY, targetCellType);
        //}

        //private bool CanKeeperMoveBox(Cell keeperCell, Cell boxCell, CellType[,] map)
        //{
        //    var targetCell = GetTargetCell(keeperCell, boxCell, map);

        //    return targetCell.CellType == CellType.EMPTY ||
        //           targetCell.CellType == CellType.LOCATION;
        //}

        //private CellType[,] MakeMove(Cell[] path, CellType[,] map)
        //{
        //    var resultMap = map.Clone() as CellType[,];

        //    var keeperCell = path[0];
        //    var boxCell = path[path.Length - 1];
        //    var targetCell = GetTargetCell(path[path.Length - 2], boxCell, map);

        //    resultMap[keeperCell.X, keeperCell.Y] = gameData.Map[keeperCell.X, keeperCell.Y] == CellType.LOCATION ? CellType.LOCATION : CellType.EMPTY;
        //    resultMap[boxCell.X, boxCell.Y] = resultMap[boxCell.X, boxCell.Y] == CellType.LOCATION ? CellType.KEEPER_ON_LOCATION : CellType.KEEPER;
        //    resultMap[targetCell.X, targetCell.Y] = resultMap[targetCell.X, targetCell.Y] == CellType.LOCATION ? CellType.BOX_ON_LOCATION : CellType.BOX;

        //    return resultMap;
        //}

        //private static List<Cell[]> GetPathsToBoxes(GameData gameData)
        //{
        //    var paths = new List<Cell[]>();

        //    foreach (var boxCell in gameData.BoxCells)
        //    {
        //        foreach (Direction touchFrom in Enum.GetValues(typeof(Direction)))
        //        {
        //            var searchParam = new SearchParameters(gameData.KeeperCell, boxCell, touchFrom, gameData.Map);
        //            var path = new PathFinder(searchParam).FindPath();

        //            if (path.Length > 1)
        //            {
        //                paths.Add(path);
        //            }
        //        }
        //    }

        //    return paths;
        //}

        //private void PopulatePossibleMoves(List<Move> moves, Cell keeperCell, CellType[,] map, ref Move currentMove)
        //{
        //    foreach (Direction direction in Enum.GetValues(typeof(Direction)))
        //    {
        //        int xOffset = direction == Direction.Left ? -1 :
        //                      direction == Direction.Up ? 0 :
        //                      direction == Direction.Right ? 1 : 0;

        //        int yOffset = direction == Direction.Left ? 0 :
        //                      direction == Direction.Up ? -1 :
        //                      direction == Direction.Right ? 0 : 1;

        //        var x = keeperCell.X + xOffset;
        //        var y = keeperCell.Y + yOffset;

        //        var cellTypeAtOffset = map[x, y];

        //        if (cellTypeAtOffset == CellType.EMPTY)
        //        {
        //            MakeMove(moves, keeperCell, ref map, ref currentMove, direction);
        //            PopulatePossibleMoves(moves, new Cell(x, y, CellType.KEEPER), map, ref currentMove);
        //        }
        //        else if (cellTypeAtOffset == CellType.BOX)
        //        {

        //        }
        //    }
        //}

        //private void MakeMove(List<Move> moves, Cell keeperCell, ref CellType[,] map, ref Move currentMove, Direction direction)
        //{
        //    AddKey(moves, ref currentMove, direction);
        //    map = map.Clone() as CellType[,];
        //}

        //private void AddKey(List<Move> moves, ref Move currentMove, Direction direction)
        //{
        //    if (currentMove == null)
        //    {
        //        currentMove = new Move();
        //        moves.Add(currentMove);
        //    }

        //    var key = direction == Direction.Left ? Key.Left :
        //              direction == Direction.Up ? Key.Up :
        //              direction == Direction.Right ? Key.Right : Key.Down;

        //    currentMove.Keys.Add(key);
        //}
    }
}
