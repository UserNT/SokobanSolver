using System.Collections.Generic;
using System.Windows.Input;

namespace Sokoban
{
    public class Move
    {
        public Move(Cell[] path, Move parent = null)
        {
            Keys = new List<Key>();

            this.Parent = parent;
        }

        public Move Parent { get; private set; }

        public IEnumerable<Key> Keys { get; private set; }
    }
}
