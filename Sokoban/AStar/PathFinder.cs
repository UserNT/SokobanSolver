using System.Collections.Generic;
using System.Windows;

namespace Sokoban.AStar
{
    public abstract class PathFinder<T>
    {
        private int width;
        private int height;
        private Node<T>[,] nodes;
        protected Node<T> startNode;
        private Node<T> endNode;

        protected abstract Point GetCellLocation(T cell);

        protected abstract int GetWidth();

        protected abstract int GetHeight();

        protected abstract Node<T> CreateNode(int x, int y, T targetCell);

        protected virtual bool CanRecursivelySearch(T currentCell, T nextCell)
        {
            return true;
        }

        /// <summary>
        /// Returns adjacent cells.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="depth">Current step of recursion. If 0 then we at StartCell</param>
        protected abstract IEnumerable<T> GetAdjacentCells(T cell, int depth);

        public T[] FindPath(T fromCell, T toCell)
        {
            InitializeNodes(toCell);
            InitializeStartEndNodes(fromCell, toCell);

            return FindAndReconstructPath();
        }

        private void InitializeNodes(T toCell)
        {
            this.width = GetWidth();
            this.height = GetHeight();

            this.nodes = new Node<T>[this.width, this.height];

            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    this.nodes[x, y] = CreateNode(x, y, toCell);
                }
            }
        }

        private void InitializeStartEndNodes(T fromCell, T toCell)
        {
            var fromCellLocation = GetCellLocation(fromCell);
            var toCellLocation = GetCellLocation(toCell);

            startNode = this.nodes[(int)fromCellLocation.X, (int)fromCellLocation.Y];
            startNode.State = NodeState.Open;

            endNode = this.nodes[(int)toCellLocation.X, (int)toCellLocation.Y];
        }

        private T[] FindAndReconstructPath()
        {
            if (!endNode.IsWalkable)
            {
                return new T[] { };
            }

            // The start node is the first entry in the 'open' list
            var path = new List<T>();
            bool success = Search(startNode, 0);
            if (success)
            {
                // If a path was found, follow the parents from the end node to build a list of locations
                var node = this.endNode;
                while (node.ParentNode != null)
                {
                    path.Add(node.Cell);
                    node = node.ParentNode;
                }

                path.Add(startNode.Cell);

                // Reverse the list so it's in the correct order when returned
                path.Reverse();
            }

            return path.ToArray();
        }
        
        /// <summary>
        /// Attempts to find a path to the destination node using <paramref name="currentNode"/> as the starting location
        /// </summary>
        /// <param name="currentNode">The node from which to find a path</param>
        /// <returns>True if a path to the destination has been found, otherwise false</returns>
        private bool Search(Node<T> currentNode, int depth)
        {
            // Set the current node to Closed since it cannot be traversed more than once
            currentNode.State = NodeState.Closed;
            var nextNodes = GetAdjacentWalkableNodes(currentNode, depth);

            // Sort by F-value so that the shortest possible routes are considered first
            nextNodes.Sort((node1, node2) => node1.F.CompareTo(node2.F));
            foreach (var nextNode in nextNodes)
            {
                // Check whether the end node has been reached
                // If not, check the next set of nodes
                if (nextNode == this.endNode)
                {
                    return true;
                }
                else if (CanRecursivelySearch(currentNode.Cell, nextNode.Cell) &&
                         Search(nextNode, depth + 1))
                {
                    return true;
                }
            }

            // The method returns false if this path leads to be a dead end
            return false;
        }
        
        /// <summary>
        /// Returns any nodes that are adjacent to <paramref name="fromNode"/> and may be considered to form the next step in the path
        /// </summary>
        /// <param name="fromNode">The node from which to return the next possible nodes in the path</param>
        /// <returns>A list of next possible nodes in the path</returns>
        private List<Node<T>> GetAdjacentWalkableNodes(Node<T> fromNode, int depth)
        {
            var walkableNodes = new List<Node<T>>();
            IEnumerable<T> nextCells = GetAdjacentCells(fromNode.Cell, depth);

            foreach (var cell in nextCells)
            {
                var location = GetCellLocation(cell);
                int x = (int)location.X;
                int y = (int)location.Y;

                // Stay within the grid's boundaries
                if (x < 0 || x >= this.width || y < 0 || y >= this.height)
                    continue;

                var node = this.nodes[x, y];
                // Ignore non-walkable nodes
                if (!node.IsWalkable)
                    continue;

                // Ignore already-closed nodes
                if (node.State == NodeState.Closed)
                    continue;

                // Already-open nodes are only added to the list if their G-value is lower going via this route.
                if (node.State == NodeState.Open)
                {
                    double traversalCost = node.GetTraversalCost(node.ParentNode.Cell);
                    double gTemp = fromNode.G + traversalCost;
                    if (gTemp < node.G)
                    {
                        node.ParentNode = fromNode;
                        walkableNodes.Add(node);
                    }
                }
                else
                {
                    // If it's untested, set the parent and flag it as 'Open' for consideration
                    node.ParentNode = fromNode;
                    node.State = NodeState.Open;
                    walkableNodes.Add(node);
                }
            }

            return walkableNodes;
        }
    }
}
