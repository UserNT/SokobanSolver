using Sokoban.AStar;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Sokoban
{
    public class Scenario
    {
        public Scenario(int box, SokobanPathItem ep)
        {
            Box = box;
            EntryPoint = ep;

            PathToEntryPoint = new List<SokobanPathItem>();
            BoxesOnTheWayToEntryPoint = new List<int>();
        }

        public int Box { get; }

        public SokobanPathItem EntryPoint { get; }

        public List<SokobanPathItem> PathToEntryPoint { get; private set; }

        public List<int> BoxesOnTheWayToEntryPoint { get; set; }

        public void FindPathToEntryPoint(SokobanMap virtualMap)
        {
            PathToEntryPoint.Clear();

            var boxPathFinder = new BoxPathFinder(virtualMap);
            var path = boxPathFinder.FindPath(virtualMap.Convert(Box), virtualMap.Convert(EntryPoint.Position));

            SokobanPathItem currentItem = null;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var item = virtualMap.GetPathItem(path[i], path[i + 1]);

                if (currentItem == null || currentItem.Key != item.Key)
                {
                    currentItem = item;
                    PathToEntryPoint.Add(currentItem);
                }
                else if (currentItem.Key == item.Key)
                {
                    currentItem.StepsToTarget++;
                }
            }
        }

        public void FindBoxesOnTheWayToEntryPoint(SokobanMap initialMap)
        {
            BoxesOnTheWayToEntryPoint.Clear();

            foreach (var pathItem in PathToEntryPoint)
            {
                int currentPosition = pathItem.Position;
                for (int step = 0; step < pathItem.StepsToTarget; step++)
                {
                    var position = initialMap.GetPosition(pathItem.Key, currentPosition);

                    var cellType = initialMap[position];

                    if (cellType == SokobanMap.BOX || cellType == SokobanMap.BOX_ON_LOCATION)
                    {
                        BoxesOnTheWayToEntryPoint.Add(position);
                    }

                    currentPosition = position;
                }
            }
        }

        public bool TryFindPathToEntryPointUsingKeeper(SokobanMap map, out SokobanPath path)
        {
            path = new SokobanPath();

            var keeperPos = map.GetKeeperPosition();
            var initialKeeperTarget = GetInitialKeeperTarget(map);



            return false;
        }

        private int GetInitialKeeperTarget(SokobanMap map)
        {
            var oppositeDirection = PathToEntryPoint[0].Key == Key.Up ? Key.Down :
                                    PathToEntryPoint[0].Key == Key.Down ? Key.Up :
                                    PathToEntryPoint[0].Key == Key.Left ? Key.Right :
                                    Key.Left;

            return map.GetPosition(oppositeDirection, PathToEntryPoint[0].Position);
        }
    }
}
