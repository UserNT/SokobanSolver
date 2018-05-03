using System.Collections.Generic;

namespace Sokoban.Network
{
    public class Area : HashSet<int>
    {
        public Area(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }
}
