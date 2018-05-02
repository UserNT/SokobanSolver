namespace Sokoban
{
    public class BoxToLocation
    {
        public BoxToLocation(Cell box, Cell location)
        {
            Box = box;
            Location = location;
        }

        public Cell Box { get; set; }

        public Cell Location { get; set; }

        public override string ToString()
        {
            return Box.ToString() + " -> " + Location.ToString();
        }
    }
}
