using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Sokoban.Network
{
    public class Manager
    {
        private const int StepsToInaccessibleKeeper = -1;
        private readonly Dictionary<int, CellStaticInfo> staticGraph = new Dictionary<int, CellStaticInfo>();
        private readonly string map;

        private readonly Dictionary<int, CellDynamicInfo> dynamicGraph = new Dictionary<int, CellDynamicInfo>();
        private readonly Dictionary<int, Area> areaGraph = new Dictionary<int, Area>();

        private readonly List<IEnumerable<int>> locationsOrder = new List<IEnumerable<int>>();

        private Manager(int width, int height, string map)
        {
            this.Width = width;
            this.Height = height;
            this.map = map;

            InitializeGraphs();
            DetectAreas();
            FilterOutsiders();
            SortLocations();
        }

        #region Properties

        public int Width { get; }

        public int Height { get; }

        private char this[int? pos]
        {
            get { return pos.HasValue ? this[pos.Value] : char.MaxValue; }
        }

        public char this[int pos]
        {
            get
            {
                CellStaticInfo cell;
                CellDynamicInfo dynCellInfo;
                if (staticGraph.TryGetValue(pos, out cell) &&
                    dynamicGraph.TryGetValue(pos, out dynCellInfo))
                {
                    if (cell.IsWall)
                    {
                        return Solver.Sokoban.WALL;
                    }
                    else if (cell.IsLocation)
                    {
                        if (dynCellInfo.HoldsBox) return Solver.Sokoban.BOX_ON_LOCATION;
                        if (cell.Position == KeeperPosition) return Solver.Sokoban.KEEPER_ON_LOCATION;
                        return Solver.Sokoban.LOCATION;
                    }
                    else if (dynCellInfo.HoldsBox)
                    {
                        return Solver.Sokoban.BOX;
                    }
                    else if (cell.Position == KeeperPosition)
                    {
                        return Solver.Sokoban.KEEPER;
                    }
                }

                return Solver.Sokoban.EMPTY;
            }
        }

        public IReadOnlyDictionary<int, CellStaticInfo> StaticGraph => staticGraph;

        public IReadOnlyDictionary<int, CellDynamicInfo> DynamicGraph => dynamicGraph;

        public IReadOnlyDictionary<int, Area> AreaGraph => areaGraph;

        public List<IEnumerable<int>> LocationsOrder => locationsOrder;

        public int KeeperPosition { get; private set; }

        #endregion

        #region Initialization

        private void InitializeGraphs()
        {
            for (int pos = 0; pos < map.Length; pos++)
            {
                InitializeStaticGraph(pos);
                InitializeDynamicGraph(pos);
            }
        }

        private void InitializeStaticGraph(int pos)
        {
            var point = Convert(pos);
            bool isWall = map[pos] == Solver.Sokoban.WALL;
            bool isLocation = map[pos] == Solver.Sokoban.LOCATION ||
                              map[pos] == Solver.Sokoban.KEEPER_ON_LOCATION ||
                              map[pos] == Solver.Sokoban.BOX_ON_LOCATION;

            var cell = new CellStaticInfo(pos, (int)point.X, (int)point.Y, isLocation, isWall);
            staticGraph.Add(pos, cell);

            var leftPos = GetPosition(Key.Left, pos);
            if (leftPos.HasValue && staticGraph.ContainsKey(leftPos.Value))
            {
                var leftCell = staticGraph[leftPos.Value];
                leftCell.Right = cell;
                cell.Left = leftCell;
            }

            var topPos = GetPosition(Key.Up, pos);
            if (topPos.HasValue && staticGraph.ContainsKey(topPos.Value))
            {
                var topCell = staticGraph[topPos.Value];
                topCell.Bottom = cell;
                cell.Top = topCell;
            }
        }

        private void InitializeDynamicGraph(int pos)
        {
            var cell = new CellDynamicInfo();

            cell.HoldsBox = map[pos] == Solver.Sokoban.BOX ||
                            map[pos] == Solver.Sokoban.BOX_ON_LOCATION;

            if (map[pos] == Solver.Sokoban.KEEPER ||
                map[pos] == Solver.Sokoban.KEEPER_ON_LOCATION)
            {
                KeeperPosition = pos;
            }

            dynamicGraph.Add(pos, cell);
        }

        #endregion

        #region Detect Areas

        private void DetectAreas()
        {
            areaGraph.Clear();

            var visited = DetectKeeperArea();
            DetectRemainAreas(visited);
        }

        private BitArray DetectKeeperArea()
        {
            var keeperCell = staticGraph[KeeperPosition];

            var neighboursScope = new Queue<Tuple<int, CellStaticInfo>>();
            neighboursScope.Enqueue(new Tuple<int, CellStaticInfo>(1, keeperCell));

            var visited = new BitArray(Width * Height);
            visited.Set(keeperCell.Position, true);

            var keeperArea = new Area(areaGraph.Keys.Count);
            keeperArea.Add(keeperCell.Position);
            areaGraph.Add(keeperArea.Id, keeperArea);

            dynamicGraph[keeperCell.Position].AreaId = keeperArea.Id;
            dynamicGraph[keeperCell.Position].StepsToKeeper = 0;

            do
            {
                var pair = neighboursScope.Dequeue();

                var stepsToKeeper = pair.Item1;
                var pos = pair.Item2;

                foreach (var neighbour in GetNeighbours(pos))
                {
                    var dynCellInfo = dynamicGraph[neighbour.Position];

                    if (!visited.Get(neighbour.Position) && CanHoldKeeper(neighbour.Position))
                    {
                        dynCellInfo.AreaId = keeperArea.Id;
                        dynCellInfo.StepsToKeeper = stepsToKeeper;

                        keeperArea.Add(neighbour.Position);
                        visited.Set(neighbour.Position, true);

                        neighboursScope.Enqueue(new Tuple<int, CellStaticInfo>(stepsToKeeper + 1, neighbour));
                    }
                }
            }
            while (neighboursScope.Any());

            return visited;
        }

        private void DetectRemainAreas(BitArray visited)
        {
            var area = new Area(areaGraph.Keys.Count);

            var neighboursScope = new Queue<CellStaticInfo>();

            foreach (var pos in staticGraph.Keys)
            {
                if (!visited.Get(pos) && CanHoldKeeper(pos))
                {
                    var posDynCellInfo = dynamicGraph[pos];

                    posDynCellInfo.AreaId = area.Id;
                    posDynCellInfo.StepsToKeeper = StepsToInaccessibleKeeper;

                    area.Add(pos);
                    neighboursScope.Enqueue(staticGraph[pos]);

                    do
                    {
                        var cell = neighboursScope.Dequeue();

                        foreach (var neighbour in GetNeighbours(cell))
                        {
                            var neighbourDynCellInfo = dynamicGraph[neighbour.Position];

                            if (!visited.Get(neighbour.Position) && CanHoldKeeper(neighbour.Position))
                            {
                                neighbourDynCellInfo.AreaId = area.Id;
                                neighbourDynCellInfo.StepsToKeeper = StepsToInaccessibleKeeper;

                                area.Add(neighbour.Position);
                                visited.Set(neighbour.Position, true);

                                neighboursScope.Enqueue(neighbour);
                            }
                        }
                    }
                    while (neighboursScope.Any());
                }

                if (area.Count > 0)
                {
                    areaGraph.Add(area.Id, area);
                    area = new Area(areaGraph.Keys.Count);
                }
            }
        }

        private IEnumerable<CellStaticInfo> GetNeighbours(CellStaticInfo cell)
        {
            if (cell.Left != null) yield return cell.Left;
            if (cell.Top != null) yield return cell.Top;
            if (cell.Right != null) yield return cell.Right;
            if (cell.Bottom != null) yield return cell.Bottom;
        }

        #endregion

        #region Filter outsiders

        private void FilterOutsiders()
        {
            for (int x = 0; x < Width; x++)
            {
                var atTop = GetPosition(x, 0);
                var atBottom = GetPosition(x, Height - 1);

                CellDynamicInfo dynCellInfo;
                if (dynamicGraph.TryGetValue(atTop.Value, out dynCellInfo) &&
                    dynCellInfo.AreaId != 0)
                {
                    RemoveArea(dynCellInfo.AreaId);
                }

                if (dynamicGraph.TryGetValue(atBottom.Value, out dynCellInfo) &&
                    dynCellInfo.AreaId != 0)
                {
                    RemoveArea(dynCellInfo.AreaId);
                }
            }

            for (int y = 0; y < Height; y++)
            {
                var atLeft = GetPosition(0, y);
                var atRight = GetPosition(Width - 1, y);

                CellDynamicInfo dynCellInfo;
                if (dynamicGraph.TryGetValue(atLeft.Value, out dynCellInfo) &&
                    dynCellInfo.AreaId != 0)
                {
                    RemoveArea(dynCellInfo.AreaId);
                }

                if (dynamicGraph.TryGetValue(atRight.Value, out dynCellInfo) &&
                    dynCellInfo.AreaId != 0)
                {
                    RemoveArea(dynCellInfo.AreaId);
                }
            }
        }

        private void RemoveArea(int areaId)
        {
            Area area;
            if (areaGraph.TryGetValue(areaId, out area))
            {
                foreach (var pos in area)
                {
                    CellStaticInfo cell;
                    if (staticGraph.TryGetValue(pos, out cell))
                    {
                        if (cell.Left != null) cell.Left.Right = null;
                        if (cell.Top != null) cell.Top.Bottom = null;
                        if (cell.Right != null) cell.Right.Left = null;
                        if (cell.Bottom != null) cell.Bottom.Top = null;

                        staticGraph.Remove(pos);
                        dynamicGraph.Remove(pos);
                    }
                }

                area.Clear();
                areaGraph.Remove(areaId);
            }
        }

        #endregion

        #region Sort Locations

        private void SortLocations()
        {
            LocationsOrder.Clear();
            var SortedLocations = new Dictionary<int, Tuple<int, int>>();

            var backupBoxPositions = dynamicGraph.Where(x => x.Value.HoldsBox)
                                                 .Select(x => x.Key)
                                                 .ToList();
            var backupKeeperPosition = KeeperPosition;

            var locationsCount = staticGraph.Where(pair => pair.Value.IsLocation).Count();

            var entryPoints = staticGraph.Where(pair => pair.Value.IsLocation)
                                         .SelectMany(pair => GetEntryPoints(pair.Value))
                                         .ToList();

            foreach (var ep in entryPoints)
            {
                var scope = new Queue<Tuple<Key, int, int>>();
                scope.Enqueue(new Tuple<Key, int, int>(ep.Key, ep.Position, 1));

                while (scope.Count > 0)
                {
                    var task = scope.Dequeue();

                    Key key = task.Item1;
                    int boxPosition = task.Item2;
                    int currentRound = task.Item3;
                    int currentStep = 1;

                    SimulateSingleBoxPosition(key, boxPosition);

                    int? targetBoxPosition;
                    while (CanMoveBox(key, boxPosition, out targetBoxPosition))
                    {
                        currentRound++;
                        MoveKeeper(key);

                        Tuple<int, int> alreadyEstimated;
                        if (SortedLocations.TryGetValue(targetBoxPosition.Value, out alreadyEstimated))
                        {
                            //if (alreadyEstimated.Item1 > currentRound || alreadyEstimated.Item2 > currentStep)
                            //{
                            //    SortedLocations[targetBoxPosition.Value] = new Tuple<int, int>(currentRound, currentStep);
                            //}
                            //else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            SortedLocations.Add(targetBoxPosition.Value, new Tuple<int, int>(currentRound, currentStep));
                        }

                        var keeperAreaId = dynamicGraph[KeeperPosition].AreaId;

                        var key1 = GetNextClockwiseKey(key);
                        var expectedKeeperPosition = GetPosition(GetOppositeKey(key1), targetBoxPosition).Value;

                        if (areaGraph[keeperAreaId].Contains(expectedKeeperPosition))
                        {
                            scope.Enqueue(new Tuple<Key, int, int>(key1, targetBoxPosition.Value, currentRound + 1));
                        }

                        var key2 = GetNextConterClockwiseKey(key);
                        expectedKeeperPosition = GetPosition(GetOppositeKey(key2), targetBoxPosition).Value;

                        if (areaGraph[keeperAreaId].Contains(expectedKeeperPosition))
                        {
                            scope.Enqueue(new Tuple<Key, int, int>(key2, targetBoxPosition.Value, currentRound + 1));
                        }

                        boxPosition = targetBoxPosition.Value;
                        currentStep++;
                    }
                }

                SimulateSingleBoxPosition(ep.Key, ep.Position);

                if (SortedLocations.Count >= locationsCount)
                {
                    break;
                }
            }

            var ordered = SortedLocations.GroupBy(pair => pair.Value).OrderByDescending(g => g.Key.Item1).ThenByDescending(g => g.Key.Item2).Select(g => g.Select(x => x.Key)).ToList();
            locationsOrder.AddRange(ordered);

            //RecoverFromBackup(backupBoxPositions, backupKeeperPosition);
        }

        private void RecoverFromBackup(List<int> backupBoxPositions, int backupKeeperPosition)
        {
            foreach (var cellInfo in dynamicGraph.Values.Where(x => x.HoldsBox))
            {
                cellInfo.HoldsBox = false;
            }

            foreach (var boxPosition in backupBoxPositions)
            {
                dynamicGraph[boxPosition].HoldsBox = true;
            }

            KeeperPosition = backupKeeperPosition;

            DetectAreas();
        }

        private void SimulateSingleBoxPosition(Key key, int boxPosition)
        {
            // simulate single box position
            foreach (var box in dynamicGraph.Values.Where(x => x.HoldsBox))
            {
                box.HoldsBox = false;
            }
            dynamicGraph[boxPosition].HoldsBox = true;
            dynamicGraph[boxPosition].StepsToKeeper = StepsToInaccessibleKeeper;

            // simulate keeper position to push box on location
            KeeperPosition = GetPosition(GetOppositeKey(key), boxPosition).Value;

            DetectAreas();
        }

        private IEnumerable<SokobanPathItem> GetEntryPoints(CellStaticInfo locationCell)
        {
            if (!locationCell.Left.IsWall && !locationCell.Left.IsLocation && !locationCell.Left.Left.IsWall && !locationCell.Left.Left.IsLocation)
            {
                yield return new SokobanPathItem() { Key = Key.Right, Position = GetPosition(Key.Left, locationCell.Position).Value };
            }

            if (!locationCell.Top.IsWall && !locationCell.Top.IsLocation && !locationCell.Top.Top.IsWall && !locationCell.Top.Top.IsLocation)
            {
                yield return new SokobanPathItem() { Key = Key.Down, Position = GetPosition(Key.Up, locationCell.Position).Value };
            }

            if (!locationCell.Right.IsWall && !locationCell.Right.IsLocation && !locationCell.Right.Right.IsWall && !locationCell.Right.Right.IsLocation)
            {
                yield return new SokobanPathItem() { Key = Key.Left, Position = GetPosition(Key.Right, locationCell.Position).Value };
            }

            if (!locationCell.Bottom.IsWall && !locationCell.Bottom.IsLocation && !locationCell.Bottom.Bottom.IsWall && !locationCell.Bottom.Bottom.IsLocation)
            {
                yield return new SokobanPathItem() { Key = Key.Up, Position = GetPosition(Key.Down, locationCell.Position).Value };
            }
        }

        #endregion

        #region GetPosition

        public Key GetOppositeKey(Key key)
        {
            return key == Key.Up ? Key.Down :
                   key == Key.Down ? Key.Up :
                   key == Key.Left ? Key.Right :
                   Key.Left;
        }

        public Key GetNextClockwiseKey(Key key)
        {
            return key == Key.Left ? Key.Up :
                   key == Key.Up ? Key.Right :
                   key == Key.Right ? Key.Down :
                   Key.Left;
        }

        public Key GetNextConterClockwiseKey(Key key)
        {
            return key == Key.Left ? Key.Down :
                   key == Key.Down ? Key.Right :
                   key == Key.Right ? Key.Up :
                   Key.Left;
        }

        public int? GetPosition(int column, int row)
        {
            var pos = row * Width + column;

            return ValidatePos(pos);
        }

        public int? GetPosition(Key key, int? currentPos)
        {
            return currentPos.HasValue ? GetPosition(key, currentPos.Value) : currentPos;
        }

        private int? GetPosition(Key key, int currentPos)
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

        private int? ValidatePos(int pos)
        {
            return (pos >= 0 && pos < map.Length) ? pos : default(int?);
        }

        private Point Convert(int pos)
        {
            var y = pos / Width;
            var x = pos - y * Width;

            return new Point(x, y);
        }

        #endregion

        #region Corner check

        public bool CanMoveBox(Key key, int boxPosition, out int? targetBoxPosition)
        {
            targetBoxPosition = GetPosition(key, boxPosition);
            if (!targetBoxPosition.HasValue)
            {
                return false;
            }

            var cellTypeAtNewPosition = this[targetBoxPosition];
            if (staticGraph[targetBoxPosition.Value].IsWall ||
                dynamicGraph[targetBoxPosition.Value].HoldsBox)
            {
                return false;
            }
            else if (staticGraph[targetBoxPosition.Value].IsLocation)
            {
                return true;
            }

            if (key == Key.Left)
            {
                if (IsLeftTopCorner(targetBoxPosition.Value) || IsLeftBottomCorner(targetBoxPosition.Value))
                {
                    return false;
                }
            }
            else if (key == Key.Up)
            {
                if (IsLeftTopCorner(targetBoxPosition.Value) || IsRightTopCorner(targetBoxPosition.Value))
                {
                    return false;
                }
            }
            else if (key == Key.Right)
            {
                if (IsRightTopCorner(targetBoxPosition.Value) || IsRightBottomCorner(targetBoxPosition.Value))
                {
                    return false;
                }
            }
            else if (key == Key.Down)
            {
                if (IsLeftBottomCorner(targetBoxPosition.Value) || IsRightBottomCorner(targetBoxPosition.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsLeftTopCorner(int position)
        {
            var left = staticGraph[position].Left;
            var top = staticGraph[position].Top;
            var leftTop = left.Top;

            return (left.IsWall || dynamicGraph[left.Position].HoldsBox) &&
                   (top.IsWall || dynamicGraph[top.Position].HoldsBox) &&
                   (leftTop.IsWall || dynamicGraph[leftTop.Position].HoldsBox);
        }

        private bool IsRightTopCorner(int position)
        {
            var right = staticGraph[position].Right;
            var top = staticGraph[position].Top;
            var rightTop = right.Top;

            return (right.IsWall || dynamicGraph[right.Position].HoldsBox) &&
                   (top.IsWall || dynamicGraph[top.Position].HoldsBox) &&
                   (rightTop.IsWall || dynamicGraph[rightTop.Position].HoldsBox);
        }

        private bool IsLeftBottomCorner(int position)
        {
            var left = staticGraph[position].Left;
            var bottom = staticGraph[position].Bottom;
            var leftBottom = left.Bottom;

            return (left.IsWall || dynamicGraph[left.Position].HoldsBox) &&
                   (bottom.IsWall || dynamicGraph[bottom.Position].HoldsBox) &&
                   (leftBottom.IsWall || dynamicGraph[leftBottom.Position].HoldsBox);
        }

        private bool IsRightBottomCorner(int position)
        {
            var right = staticGraph[position].Right;
            var bottom = staticGraph[position].Bottom;
            var rightBottom = right.Bottom;

            return (right.IsWall || dynamicGraph[right.Position].HoldsBox) &&
                   (bottom.IsWall || dynamicGraph[bottom.Position].HoldsBox) &&
                   (rightBottom.IsWall || dynamicGraph[rightBottom.Position].HoldsBox);
        }

        #endregion

        #region Movements

        public bool HandleKeyDown(Key key)
        {
            if (Solver.Sokoban.SupportedKeys.Contains(key))
            {
                if (CanMoveKeeper(key))
                {
                    MoveKeeper(key);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CanHoldKeeper(int pos)
        {
            if (staticGraph[pos].IsWall ||
                dynamicGraph[pos].HoldsBox)
            {
                return false;
            }

            return true;
        }

        private bool CanMoveKeeper(Key key)
        {
            var targetKeeperPos = GetPosition(key, KeeperPosition);

            CellDynamicInfo keeperDynCellInfo, targetKeeperDynCellInfo;
            if (dynamicGraph.TryGetValue(KeeperPosition, out keeperDynCellInfo) &&
                dynamicGraph.TryGetValue(targetKeeperPos.Value, out targetKeeperDynCellInfo))
            {
                if (CanHoldKeeper(targetKeeperPos.Value))
                {
                    return true;
                }
                else if (targetKeeperDynCellInfo.HoldsBox)
                {
                    int? targetBoxPos;
                    if (CanMoveBox(key, targetKeeperPos.Value, out targetBoxPos))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void MoveKeeper(Key key)
        {
            var targetKeeperPos = GetPosition(key, KeeperPosition);

            CellDynamicInfo keeperDynCellInfo, targetKeeperDynCellInfo;
            if (dynamicGraph.TryGetValue(KeeperPosition, out keeperDynCellInfo) &&
                dynamicGraph.TryGetValue(targetKeeperPos.Value, out targetKeeperDynCellInfo))
            {
                if (CanHoldKeeper(targetKeeperPos.Value))
                {
                    KeeperPosition = targetKeeperPos.Value;
                    DetectAreas();
                }
                else if (targetKeeperDynCellInfo.HoldsBox)
                {
                    var targetBoxPos = GetPosition(key, targetKeeperPos);

                    CellDynamicInfo targetBoxDynCellInfo;
                    if (targetBoxPos.HasValue &&
                        dynamicGraph.TryGetValue(targetBoxPos.Value, out targetBoxDynCellInfo))
                    {
                        dynamicGraph[targetBoxPos.Value].HoldsBox = true;
                        dynamicGraph[targetBoxPos.Value].StepsToKeeper = StepsToInaccessibleKeeper;

                        targetKeeperDynCellInfo.HoldsBox = false;

                        KeeperPosition = targetKeeperPos.Value;
                        DetectAreas();
                    }
                }
            }
        }

        #endregion

        #region Using

        public static Manager Using(string gameData)
        {
            if (gameData.Contains('{'))
            {
                var responseData = JsonConvert.DeserializeObject<Response>(gameData);

                gameData = responseData.Data.GameData;
            }

            var parts = gameData.Split('*');

            return Using(int.Parse(parts[1]), int.Parse(parts[0]), parts[2]);
        }

        public static Manager Using(int width, int height, string map)
        {
            return new Manager(width, height, map);
        }

        #endregion
    }
}
