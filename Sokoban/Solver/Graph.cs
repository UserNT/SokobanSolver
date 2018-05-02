using Sokoban.AStar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Sokoban.Solver
{
    public static class Graph
    {
        public static Dictionary<int, LocationGroup> GetLocationGroups(INavigator navigator)
        {
            var locationGroups = new Dictionary<int, LocationGroup>();

            navigator.Foreach(new[] { Sokoban.LOCATION, Sokoban.KEEPER_ON_LOCATION, Sokoban.BOX_ON_LOCATION }, (areaId, position) =>
            {
                if (!locationGroups.ContainsKey(areaId))
                {
                    locationGroups.Add(areaId, new LocationGroup(areaId) { });
                }

                locationGroups[areaId].Positions.Add(position);

                foreach (var key in Sokoban.SupportedKeys)
                {
                    var neighbor1 = navigator.GetPosition(key, position);

                    if (neighbor1.HasValue &&
                        (navigator[neighbor1.Value] == Sokoban.EMPTY ||
                         navigator[neighbor1.Value] == Sokoban.KEEPER ||
                         navigator[neighbor1.Value] == Sokoban.BOX))
                    {
                        var neighbor2 = navigator.GetPosition(key, neighbor1.Value);

                        if (neighbor2.HasValue &&
                            (navigator[neighbor2.Value] == Sokoban.EMPTY ||
                             navigator[neighbor2.Value] == Sokoban.KEEPER ||
                             navigator[neighbor2.Value] == Sokoban.BOX))
                        {
                            var oppositeKey = key == Key.Up ? Key.Down :
                                              key == Key.Down ? Key.Up :
                                              key == Key.Left ? Key.Right :
                                              Key.Left;

                            locationGroups[areaId].EntryPoints.Add(new SokobanPathItem()
                            {
                                Position = neighbor1.Value,
                                Key = oppositeKey,
                                StepsToTarget = 1
                            });
                        }
                    }
                }
            });

            return locationGroups;
        }

        public static Stack<List<int>> GetFillingSteps(INavigator navigator, LocationGroup locationGroup)
        {
            var result = new Stack<List<int>>();

            var antiNavigator = navigator.ReplaceWithBoxes(locationGroup);

            var scope = new Queue<INavigator>();
            scope.Enqueue(antiNavigator);

            while (scope.Count > 0)
            {
                var currentNavigator = scope.Dequeue();

                var step = new List<int>();

                foreach (var entryPoint in locationGroup.EntryPoints)
                {
                    var boxesToExit = GetBoxesToExit(currentNavigator, entryPoint);

                    step.AddRange(boxesToExit.Except(step));
                }

                if (step.Count > 0)
                {
                    var navigatorAfterStep = currentNavigator.Replace(step, Sokoban.LOCATION);

                    scope.Enqueue(navigatorAfterStep);
                    result.Push(step);
                }
            }

            return result;
        }

        private static List<int> GetBoxesToExit(INavigator navigator, SokobanPathItem entryPoint)
        {
            var boxes = new List<int>();

            //var areasToBoxTouchPoints = GetAreasToBoxesTouchPoints(navigator);
            //var keeperPos = navigator.GetKeeperPosition();
            //var keeperArea = areasToBoxTouchPoints.Where(x => x.Value.Positions.Contains(keeperPos)).First();

            navigator.Foreach(new[] { Sokoban.BOX_ON_LOCATION }, (box) =>
            {
                var keeperPos = navigator.GetKeeperPosition();
                var keeperTarget = navigator.GetPosition(navigator.GetOppositeKey(entryPoint.Key), keeperPos);
                var targetBoxPos = entryPoint.Position;

                if (navigator.CanDrag(keeperPos, box, keeperTarget.Value, targetBoxPos))
                {
                    boxes.Add(box);
                }

                //var keeperToBoxTouchPoints = keeperArea.Value.EntryPoints.Where(ep => navigator.GetPosition(ep.Key, ep.Position).Value == box).ToList();

                //var isKeeperCanTouchBox = keeperToBoxTouchPoints.Count > 0;

                //if (isKeeperCanTouchBox && CanDrag(box, navigator, entryPoint, keeperToBoxTouchPoints))
                //{
                //    boxes.Add(box);
                //}
            });

            return boxes;
        }

        public static Dictionary<int, LocationGroup> GetAreasToBoxesTouchPoints(INavigator navigator)
        {
            var areasToBoxTouchPoints = new Dictionary<int, LocationGroup>();

            navigator.Foreach(new[] { Sokoban.EMPTY, Sokoban.LOCATION, Sokoban.KEEPER, Sokoban.KEEPER_ON_LOCATION }, (areaId, position) =>
            {
                if (!areasToBoxTouchPoints.ContainsKey(areaId))
                {
                    areasToBoxTouchPoints.Add(areaId, new LocationGroup(areaId) { });
                }

                areasToBoxTouchPoints[areaId].Positions.Add(position);

                navigator.ForeachNeighbors(position, (neighbor, key, cellType) =>
                {
                    if (neighbor.HasValue && (cellType == Sokoban.BOX || cellType == Sokoban.BOX_ON_LOCATION))
                    {
                        areasToBoxTouchPoints[areaId].EntryPoints.Add(new SokobanPathItem()
                        {
                            Position = position, //neighbor.Value,
                            Key = key, //navigator.GetOppositeKey(key),
                            StepsToTarget = 1
                        });
                    }
                });
            });

            return areasToBoxTouchPoints;
        }

        public static bool CanDrag(int box, INavigator navigator, SokobanPathItem toExitPoint, List<SokobanPathItem> fromKeeperToBoxTouchPoints)
        {
            foreach (var fromKeeperToBoxTouchPoint in fromKeeperToBoxTouchPoints)
            {

            }

            return false;
        }

        //public static bool CanDrag(int box, INavigator navigator, SokobanPathItem toExitPoint, List<SokobanPathItem> fromKeeperToBoxTouchPoints)
        //{
        //    foreach (var fromKeeperToBoxTouchPoint in fromKeeperToBoxTouchPoints)
        //    {
        //        if (fromKeeperToBoxTouchPoint.Position == toExitPoint.Position)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            var processedStates = new HashSet<INavigator>();
        //            var scope = new Queue<INavigator>();
        //            scope.Enqueue(navigator);

        //            while (scope.Count > 0)
        //            {
        //                var currentNavigator = scope.Dequeue();

        //                var keeperPos = fromKeeperToBoxTouchPoint.Position;
        //                var boxTarget = keeperPos;
        //                var keeperTarget = currentNavigator.GetPosition(currentNavigator.GetOppositeKey(fromKeeperToBoxTouchPoint.Key), boxTarget);

        //                if (keeperTarget.HasValue &&
        //                    (currentNavigator[keeperTarget.Value] == Sokoban.EMPTY ||
        //                     currentNavigator[keeperTarget.Value] == Sokoban.LOCATION))
        //                {
        //                    var newNavigator = currentNavigator.Drag(keeperPos, keeperTarget.Value, box);
        //                    if (processedStates.Add(newNavigator))
        //                    {
        //                        scope.Enqueue(newNavigator);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return true;
        //}

        public static IEnumerable<LocationItem> GetFillingOrder(INavigator navigator, IEnumerable<LocationGroup> locationGroups)
        {
            var items = new List<LocationItem>();

            int currentRound = 1;
            foreach (var entryPoint in locationGroups.SelectMany(g => g.EntryPoints))
            {
                var key = entryPoint.Key;
                int? neighbor = entryPoint.Position;
                int currentStep = 1;

                bool canContinue = false;
                do
                {
                    var keeperPos = neighbor;
                    neighbor = navigator.GetPosition(key, keeperPos);

                    if (neighbor.HasValue &&
                        (navigator[neighbor.Value] == Sokoban.EMPTY ||
                         navigator[neighbor.Value] == Sokoban.KEEPER ||
                         navigator[neighbor.Value] == Sokoban.LOCATION ||
                         navigator[neighbor.Value] == Sokoban.KEEPER_ON_LOCATION ||
                         navigator[neighbor.Value] == Sokoban.BOX_ON_LOCATION))
                    {
                        if (navigator[neighbor.Value] == Sokoban.LOCATION ||
                            navigator[neighbor.Value] == Sokoban.KEEPER_ON_LOCATION ||
                            navigator[neighbor.Value] == Sokoban.BOX_ON_LOCATION)
                        {
                            items.Add(new LocationItem() { KeeperPos = keeperPos.Value, Round = currentRound, Step = currentStep, Position = neighbor.Value });
                            currentStep++;
                        }

                        canContinue = true;
                    }
                    else
                    {
                        canContinue = false;
                    }
                }
                while (canContinue);
            }

            //items.Add(new LocationItem() { Round = 1, Step = 1, Position = 53 });
            //items.Add(new LocationItem() { Round = 1, Step = 2, Position = 54 });
            //items.Add(new LocationItem() { Round = 1, Step = 3, Position = 55 });

            //items.Add(new LocationItem() { Round = 2, Step = 1, Position = 64 });
            //items.Add(new LocationItem() { Round = 2, Step = 1, Position = 65 });
            //items.Add(new LocationItem() { Round = 2, Step = 2, Position = 75 });
            //items.Add(new LocationItem() { Round = 2, Step = 3, Position = 85 });

            //items.Add(new LocationItem() { Round = 3, Step = 1, Position = 63 });

            return items.OrderByDescending(l => l.Round).ThenByDescending(l => l.Step).ToList();
        }
    }
}
