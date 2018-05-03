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
        private int keeperPosition;

        private Manager(int width, int height, string map)
        {
            this.Width = width;
            this.Height = height;
            this.map = map;

            InitializeGraphs();
            DetectAreas();
            FilterOutsiders();
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
                        if (dynCellInfo.HoldsKeeper) return Solver.Sokoban.KEEPER_ON_LOCATION;
                        return Solver.Sokoban.LOCATION;
                    }
                    else if (dynCellInfo.HoldsBox)
                    {
                        return Solver.Sokoban.BOX;
                    }
                    else if (dynCellInfo.HoldsKeeper)
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

            cell.CanHoldBox = map[pos] != Solver.Sokoban.WALL &&
                              !IsLeftTopCorner(pos) &&
                              !IsRightTopCorner(pos) &&
                              !IsLeftBottomCorner(pos) &&
                              !IsRightBottomCorner(pos);

            cell.CanHoldKeeper = map[pos] != Solver.Sokoban.WALL &&
                                 map[pos] != Solver.Sokoban.BOX &&
                                 map[pos] != Solver.Sokoban.BOX_ON_LOCATION;

            cell.HoldsBox = map[pos] == Solver.Sokoban.BOX ||
                            map[pos] == Solver.Sokoban.BOX_ON_LOCATION;

            cell.HoldsKeeper = map[pos] == Solver.Sokoban.KEEPER ||
                               map[pos] == Solver.Sokoban.KEEPER_ON_LOCATION;

            if (cell.HoldsKeeper)
            {
                keeperPosition = pos;
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
            var keeperCell = staticGraph[keeperPosition];

            var neighboursScope = new Queue<Tuple<int, CellStaticInfo>>();
            neighboursScope.Enqueue(new Tuple<int, CellStaticInfo>(1, keeperCell));

            var visited = new BitArray(Width * Height);
            visited.Set(keeperCell.Position, true);

            var keeperArea = new Area(areaGraph.Keys.Count);
            keeperArea.Add(keeperCell.Position);
            areaGraph.Add(keeperArea.Id, keeperArea);

            dynamicGraph[keeperCell.Position].StepsToKeeper = 0;

            do
            {
                var pair = neighboursScope.Dequeue();

                var stepsToKeeper = pair.Item1;
                var pos = pair.Item2;

                foreach (var neighbour in GetNeighbours(pos))
                {
                    var dynCellInfo = dynamicGraph[neighbour.Position];

                    if (!visited.Get(neighbour.Position) && dynCellInfo.CanHoldKeeper)
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
                if (!visited.Get(pos) && dynamicGraph[pos].CanHoldKeeper)
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

                            if (!visited.Get(neighbour.Position) && neighbourDynCellInfo.CanHoldKeeper)
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

        #region GetPosition

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

        private bool IsLeftTopCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var top = GetPosition(Key.Up, position);
            var leftTop = GetPosition(Key.Up, left);

            return (this[left] == Solver.Sokoban.WALL || this[left] == Solver.Sokoban.BOX) &&
                   (this[top] == Solver.Sokoban.WALL || this[top] == Solver.Sokoban.BOX) &&
                   (this[leftTop] == Solver.Sokoban.WALL || this[leftTop] == Solver.Sokoban.BOX);
        }

        private bool IsRightTopCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var top = GetPosition(Key.Up, position);
            var rightTop = GetPosition(Key.Up, right);

            return (this[right] == Solver.Sokoban.WALL || this[right] == Solver.Sokoban.BOX) &&
                   (this[top] == Solver.Sokoban.WALL || this[top] == Solver.Sokoban.BOX) &&
                   (this[rightTop] == Solver.Sokoban.WALL || this[rightTop] == Solver.Sokoban.BOX);
        }

        private bool IsLeftBottomCorner(int position)
        {
            var left = GetPosition(Key.Left, position);
            var bottom = GetPosition(Key.Down, position);
            var leftBottom = GetPosition(Key.Down, left);

            return (this[left] == Solver.Sokoban.WALL || this[left] == Solver.Sokoban.BOX) &&
                   (this[bottom] == Solver.Sokoban.WALL || this[bottom] == Solver.Sokoban.BOX) &&
                   (this[leftBottom] == Solver.Sokoban.WALL || this[leftBottom] == Solver.Sokoban.BOX);
        }

        private bool IsRightBottomCorner(int position)
        {
            var right = GetPosition(Key.Right, position);
            var bottom = GetPosition(Key.Down, position);
            var rightBottom = GetPosition(Key.Down, right);

            return (this[right] == Solver.Sokoban.WALL || this[right] == Solver.Sokoban.BOX) &&
                   (this[bottom] == Solver.Sokoban.WALL || this[bottom] == Solver.Sokoban.BOX) &&
                   (this[rightBottom] == Solver.Sokoban.WALL || this[rightBottom] == Solver.Sokoban.BOX);
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

        private bool CanMoveKeeper(Key key)
        {
            var targetPos = GetPosition(key, keeperPosition);

            CellDynamicInfo keeperDynCellInfo, targetDynCellInfo;
            if (dynamicGraph.TryGetValue(keeperPosition, out keeperDynCellInfo) &&
                dynamicGraph.TryGetValue(targetPos.Value, out targetDynCellInfo))
            {
                if (targetDynCellInfo.CanHoldKeeper)
                {
                    return true;
                }
                else if (targetDynCellInfo.HoldsBox)
                {

                }
            }

            return false;
        }

        private void MoveKeeper(Key key)
        {
            var targetPos = GetPosition(key, keeperPosition);

            CellDynamicInfo keeperDynCellInfo, targetDynCellInfo;
            if (dynamicGraph.TryGetValue(keeperPosition, out keeperDynCellInfo) &&
                dynamicGraph.TryGetValue(targetPos.Value, out targetDynCellInfo))
            {
                if (targetDynCellInfo.CanHoldKeeper)
                {
                    keeperDynCellInfo.HoldsKeeper = false;
                    targetDynCellInfo.HoldsKeeper = true;

                    keeperPosition = targetPos.Value;

                    DetectAreas();
                }
                else if (targetDynCellInfo.HoldsBox)
                {

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
