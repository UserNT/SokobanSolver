using System.Collections.Generic;
using System.Windows.Input;

namespace Sokoban.Network
{
    public class Cell
    {
        public Cell()
        {
            Neighbors = new Dictionary<Key, Cell>();
        }

        public bool CanHoldKeeper { get; }

        public bool CanHoldBox { get; }

        public bool HoldsKeeper { get; }

        public bool HoldsBox { get; }

        public bool IsLocation { get; }

        public int Position { get; }

        public Dictionary<Key, Cell> Neighbors { get; }
    }
}
