using System.Windows.Input;

namespace Sokoban
{
    public class SokobanPathItem
    {
        public int Position { get; set; }

        public Key Key { get; set; }

        public int StepsToTarget { get; set; }
    }
}
