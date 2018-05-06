using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sokoban.Network;
using System.Windows.Input;

namespace Sokoban.Tests
{
    [TestClass]
    public class ManagerTest
    {
        private static readonly Key[] SupportedKeys = new Key[] { Key.Left, Key.Up, Key.Right, Key.Down };

        private const char EMPTY = '0';
        private const char KEEPER = '1';
        private const char BOX = '2';
        private const char WALL = '3';
        private const char LOCATION = '4';
        private const char BOX_ON_LOCATION = '5';
        private const char KEEPER_ON_LOCATION = '6';

        [TestMethod]
        public void Ctor_ShouldInitializeStaticGraph()
        {
            var manager = Manager.Using(Sokoban.Solver.Sokoban.Level1);
        }
    }
}
