using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sokoban.Solver;
using System.Windows.Input;
using System.Collections.Generic;

namespace Sokoban.Tests
{
    [TestClass]
    public class NavigatorTest
    {
        private static readonly Key[] SupportedKeys = new Key[] { Key.Left, Key.Up, Key.Right, Key.Down };

        private const char EMPTY = '0';
        private const char KEEPER = '1';
        private const char BOX = '2';
        private const char WALL = '3';
        private const char LOCATION = '4';
        private const char BOX_ON_LOCATION = '5';
        private const char KEEPER_ON_LOCATION = '6';

        [TestMethod]
        public void TestMethod1()
        {
            var navigator = Navigator.Using(Solver.Sokoban.Level2);
            var locationGroups = Graph.GetLocationGroups(navigator);

            var order = Graph.GetFillingOrder(navigator, locationGroups.Values);
            //var steps = Graph.GetFillingSteps(navigator, locationGroups.Last().Value);

            int locations = 0;

            //navigator.ForeachNeighborsRecursively(navigator.GetKeeperPosition(), (neighbor, key, cellType) => 
            //{
            //    if (cellType == Solver.Sokoban.LOCATION)
            //    {
            //        locations++;
            //    }

            //    return locations < 3;
            //});
        }

        [TestMethod]
        public void BuildDependencyGraph()
        {
            var moveKeeperGraph = new Dictionary<int, List<SokobanPathItem>>();
            var moveBoxGraph = new Dictionary<int, List<SokobanPathItem>>();

            var navigator = Navigator.Using(Solver.Sokoban.Level10);
            navigator.Foreach(new[] { EMPTY, KEEPER, BOX, LOCATION, BOX_ON_LOCATION, KEEPER_ON_LOCATION }, (position) =>
            {
                foreach (var key in SupportedKeys)
                {
                    var neighbor = navigator.GetPosition(key, position);

                    if (neighbor.HasValue && navigator[neighbor.Value] != WALL)
                    {
                        AddEntryPoint(moveKeeperGraph, position, key, neighbor.Value);

                        var boxTarget = navigator.GetPosition(key, neighbor.Value);

                        if (boxTarget.HasValue && navigator[boxTarget.Value] != WALL)
                        {
                            AddEntryPoint(moveBoxGraph, neighbor.Value, key, boxTarget.Value);
                        }
                    }
                }
            });

            var pos = moveKeeperGraph.Keys.Except(moveBoxGraph.Keys).ToList();
        }

        private void AddEntryPoint(Dictionary<int, List<SokobanPathItem>> graph, int position, Key key, int neighbor)
        {
            var entryPoint = new SokobanPathItem()
            {
                Position = position,
                Key = key,
                StepsToTarget = 1
            };

            if (!graph.ContainsKey(neighbor))
            {
                graph.Add(neighbor, new List<SokobanPathItem>());
            }

            graph[neighbor].Add(entryPoint);
        }
    }
}
