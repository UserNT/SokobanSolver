namespace Sokoban.AStar
{
    /// <summary>
    /// Represents a single node on a grid that is being searched for a path between two points
    /// </summary>
    public abstract class Node<T>
    {
        private Node<T> parentNode;

        /// <summary>
        /// The node's location in the grid
        /// </summary>
        public T Cell { get; private set; }

        /// <summary>
        /// True when the node may be traversed, otherwise false
        /// </summary>
        public bool IsWalkable { get; private set; }

        /// <summary>
        /// Cost from start to here
        /// </summary>
        public double G { get; private set; }

        /// <summary>
        /// Estimated cost from here to end
        /// </summary>
        public double H { get; private set; }

        /// <summary>
        /// Estimated total cost (F = G + H)
        /// </summary>
        public double F
        {
            get { return this.G + this.H; }
        }

        /// <summary>
        /// Flags whether the node is open, closed or untested by the PathFinder
        /// </summary>
        public NodeState State { get; set; }

        /// <summary>
        /// Gets or sets the parent node. The start node's parent is always null.
        /// </summary>
        public Node<T> ParentNode
        {
            get { return this.parentNode; }
            set
            {
                // When setting the parent, also calculate the traversal cost from the start node to here (the 'G' value)
                this.parentNode = value;
                this.G = this.parentNode.G + GetTraversalCost(this.parentNode.Cell);
            }
        }

        /// <summary>
        /// Creates a new instance of Node.
        /// </summary>
        /// <param name="cell">The node's location in the grid</param>
        /// <param name="isWalkable">True if the node can be traversed, false if the node is a wall</param>
        /// <param name="endLocation">The location of the destination node</param>
        protected Node(T cell, bool isWalkable, T targetCell)
        {
            this.Cell = cell;
            this.State = NodeState.Untested;
            this.IsWalkable = isWalkable;
            this.H = GetTraversalCost(targetCell);
            this.G = 0;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Cell, this.State);
        }

        /// <summary>
        /// Gets the distance between two points
        /// </summary>
        public abstract double GetTraversalCost(T toCell);
    }
}
