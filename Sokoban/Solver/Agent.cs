using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sokoban.Solver
{
    public class Agent : IAgent
    {
        private readonly ColorMapControl ctrl;
        private int height;
        private int width;
        private string map;

        public Agent(ColorMapControl ctrl, string gameData)
        {
            this.ctrl = ctrl;
            var parts = gameData.Split('*');

            height = int.Parse(parts[0]);
            width = int.Parse(parts[1]);
            map = parts[2];

            this.ctrl.ColorMap = new ColorMap(width, height, parts[2]);
        }

        public Task SolveAsync()
        {
            var currentState = ctrl.ColorMap;

            return Task.Run(() =>
            {
                Solve(currentState);
            });
        }
        
        private readonly HashSet<string> stateHashes = new HashSet<string>();

        private bool Solve(ColorMap currentState)
        {
            var touchPoints = GetAvailableMoves(currentState).ToList();

            foreach (var touchPoint in touchPoints)
            {
                ColorMap newState = currentState.MoveKeeper(touchPoint);

                if (!stateHashes.Add(newState.ToString()))
                {
                    continue;
                }

                if (newState.IsComplete())
                {
                    ctrl.Dispatcher.Invoke((Action)(() =>
                    {
                        ctrl.ColorMap = currentState;
                    }));

                    return true;
                }
                else if (Solve(newState))
                {
                    ctrl.Dispatcher.Invoke((Action)(() =>
                    {
                        ctrl.ColorMap = currentState;
                    }));

                    return true;
                }
            }

            ctrl.Dispatcher.Invoke((Action)(() =>
            {
                ctrl.ColorMap = currentState;
            }));

            return false;
        }

        private IEnumerable<SokobanPathItem> GetAvailableMoves(ColorMap colorMap)
        {
            return colorMap.ColorToBoxTouchPoints
                           .Where(pair => colorMap.ColorToKeeperTouchPoint.Keys.Contains(pair.Key))
                           .SelectMany(x => x.Value);
        }
    }
}
