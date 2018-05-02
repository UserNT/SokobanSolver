using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Sokoban.Solver
{
    public interface INavigator
    {
        int Width { get; }

        int Height { get; }

        char this[int pos] { get; }

        bool CanHoldKeeper(int? pos);

        int? GetPosition(int column, int row);

        int? GetPosition(Key key, int? currentPos);

        int? GetPosition(Key key, int currentPos);

        int GetKeeperPosition();

        double GetDistance(int pos1, int pos2);

        Key GetOppositeKey(Key key);

        Point Convert(int pos);

        void Foreach(char[] cellTypes, Action<int> callback);

        void ForeachNeighbors(int position, Action<int?, Key, char> callback);

        void Foreach(char[] cellTypes, Action<int, int> callback);

        void ForeachNeighborsRecursively(int position, Func<int, Key, int, char, bool> canContinue);

        bool CanPush(Key key, int? keeperPos = null);

        INavigator Push(Key key, int? keeperPos = null);

        bool CanDrag(Key key, int? keeperPos = null);

        bool CanDrag(int keeperStartPos, int boxStartPos, int keeperEndPos, int boxEndPos);

        INavigator Drag(Key key, int? keeperPos = null);

        INavigator Drag(int keeperPos, int keeperTarget, int boxPos);

        INavigator ReplaceWithBoxes(LocationGroup group);

        INavigator Replace(IEnumerable<int> positions, char cellType);
    }
}
