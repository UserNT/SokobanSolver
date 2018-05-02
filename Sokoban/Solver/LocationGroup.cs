using System.Collections.Generic;

namespace Sokoban.Solver
{
    public class LocationGroup
    {
        public LocationGroup(int id)
        {
            Id = id;
            Positions = new List<int>();
            EntryPoints = new List<SokobanPathItem>();
        }

        public int Id { get; }

        public List<int> Positions { get; }

        public List<SokobanPathItem> EntryPoints { get; }
    }
}
