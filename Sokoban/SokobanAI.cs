using Sokoban.AStar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sokoban
{
    public class SokobanAI
    {
        private readonly SokobanMap map;
        private readonly string mapAsString;

        public SokobanAI(int width, int height, string map)
        {
            this.map = new SokobanMap(width, height, map);
            this.mapAsString = map;
        }

        public IEnumerable<IGrouping<int, Scenario>> FindScenatios()
        {
            var keeperPos = map.GetKeeperPosition();
            var entryPoints = map.FindEntryPoints();
            var boxPositions = map.FindBoxPositions();

            var scenarios = new List<Scenario>();

            foreach (var box in boxPositions)
            {
                var virtualMap = CreateVirtualMap(box);

                foreach (var ep in entryPoints)
                {
                    var scenario = new Scenario(box, ep);
                    scenario.FindPathToEntryPoint(virtualMap);
                    scenario.FindBoxesOnTheWayToEntryPoint(map);

                    scenarios.Add(scenario);
                }
            }

            return scenarios.OrderBy(s => s.BoxesOnTheWayToEntryPoint.Count)
                            .GroupBy(s => s.Box);
        }

        public bool TryFindPathToEntryPointUsingKeeper(Scenario scenario, out SokobanPath path)
        {
            return scenario.TryFindPathToEntryPointUsingKeeper(map, out path);
        }

        public void Move(SokobanPath path)
        {

        }

        private SokobanMap CreateVirtualMap(int box)
        {
            var cells = mapAsString.Replace(SokobanMap.BOX, SokobanMap.EMPTY)
                                   .Replace(SokobanMap.BOX_ON_LOCATION, SokobanMap.LOCATION)
                                   .ToArray();

            cells[box] = (mapAsString[box] == SokobanMap.BOX) ? SokobanMap.BOX : SokobanMap.BOX_ON_LOCATION;

            return new SokobanMap(map.Width, map.Height, new string(cells));
        }
    }
}
