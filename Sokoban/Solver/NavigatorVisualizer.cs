using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sokoban.Solver
{
    public class NavigatorVisualizer : Control
    {
        private Dictionary<char, Brush> brushes;

        private Grid rootGrid;

        public static readonly DependencyProperty NavigatorProperty = DependencyProperty.Register("Navigator", typeof(INavigator), typeof(NavigatorVisualizer), new PropertyMetadata(null, OnNavigatorPropertyChanged));

        public INavigator Navigator
        {
            get { return (INavigator)GetValue(NavigatorProperty); }
            set { SetValue(NavigatorProperty, value); }
        }

        private static void OnNavigatorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as NavigatorVisualizer;
            ctrl?.OnNavigatorPropertyChanged();
        }

        public NavigatorVisualizer()
        {
            InitializeBrushes();
        }

        public override void OnApplyTemplate()
        {
            rootGrid = GetTemplateChild("rootGrid") as Grid;
        }

        private void InitializeBrushes()
        {
            brushes = typeof(Colors).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                                    .Select(pi => (Color)pi.GetValue(null))
                                    .Where(c => c != Colors.Red && c != Colors.Blue && c != Colors.Lime)
                                    .OrderBy(pi => Guid.NewGuid())
                                    .Select((c, i) => new { key = Convert.ToChar(i), Value = c })
                                    .ToDictionary(x => x.key, x => (new SolidColorBrush(x.Value)) as Brush);

            brushes[Sokoban.BOX] = GetImageBrush("box.gif");
            brushes[Sokoban.BOX_ON_LOCATION] = GetImageBrush("box_on_location.gif");
            brushes[Sokoban.KEEPER] = GetImageBrush("keeper.gif");
            brushes[Sokoban.KEEPER_ON_LOCATION] = GetImageBrush("keeper.gif");
            brushes[Sokoban.LOCATION] = GetImageBrush("location.gif");
            brushes[Sokoban.WALL] = GetImageBrush("wall.gif");
        }

        private Brush GetImageBrush(string resourceName)
        {
            return new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Sokoban;component/Resources/" + resourceName)));
        }

        private void OnNavigatorPropertyChanged()
        {
            if (Navigator == null)
            {
                rootGrid.Children.Clear();
                rootGrid.RowDefinitions.Clear();
                rootGrid.ColumnDefinitions.Clear();
                return;
            }

            if (rootGrid.ColumnDefinitions.Count == 0)
            {
                for (int column = 0; column < Navigator.Width; column++)
                {
                    rootGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
            }

            if (rootGrid.RowDefinitions.Count == 0)
            {
                for (int row = 0; row < Navigator.Height; row++)
                {
                    rootGrid.RowDefinitions.Add(new RowDefinition());
                }
            }

            DrawElements();
        }

        private void DrawElements()
        {
            rootGrid.Children.Clear();

            //var moveKeeperGraph = new Dictionary<int, List<SokobanPathItem>>();
            //var moveBoxGraph = new Dictionary<int, List<SokobanPathItem>>();
            //var locationsGraph = new Dictionary<int, List<SokobanPathItem>>();

            //Navigator.Foreach(new[] { Sokoban.EMPTY, Sokoban.KEEPER, Sokoban.BOX, Sokoban.LOCATION, Sokoban.BOX_ON_LOCATION, Sokoban.KEEPER_ON_LOCATION }, (position) =>
            //{
            //    foreach (var key in Sokoban.SupportedKeys)
            //    {
            //        var neighbor = Navigator.GetPosition(key, position);

            //        if (neighbor.HasValue && Navigator[neighbor.Value] != Sokoban.WALL)
            //        {
            //            AddEntryPoint(moveKeeperGraph, position, key, neighbor.Value);

            //            var boxTarget = Navigator.GetPosition(key, neighbor.Value);

            //            if (boxTarget.HasValue && Navigator[boxTarget.Value] != Sokoban.WALL)
            //            {
            //                AddEntryPoint(moveBoxGraph, neighbor.Value, key, boxTarget.Value);
            //            }
            //        }

            //        //if (navigator[position] == Sokoban.LOCATION || navigator[position] == Sokoban.KEEPER_ON_LOCATION || navigator[position] == Sokoban.BOX_ON_LOCATION)
            //        //{
            //        //    locationsGraph.a
            //        //}
            //    }
            //});

            var locationGroups = Solver.Graph.GetLocationGroups(Navigator);
            //var anyGroup = locationGroups.First().Value;
            //var steps = Graph.GetFillingSteps(Navigator, anyGroup);
            //var order = new List<List<int>>(steps.Reverse());

            var order = Graph.GetFillingOrder(Navigator, locationGroups.Values);

            for (int row = 0; row < Navigator.Height; row++)
            {
                for (int column = 0; column < Navigator.Width; column++)
                {
                    var pos = Navigator.GetPosition(column, row);
                    var colorIndex = Navigator[pos.Value];

                    var border = new Border();
                    border.Background = brushes[colorIndex];

                    var locationItem = order.FirstOrDefault(x => x.Position == pos);

                    if (locationItem != null)
                    {
                        var text = locationItem.Round + " / " + locationItem.Step;

                        var viewBox = new Viewbox();
                        viewBox.Stretch = Stretch.Uniform;
                        viewBox.Child = new TextBlock() { Text = text, Foreground = new SolidColorBrush(Colors.Black) };
                        border.Child = viewBox;
                    }

                    border.SetValue(Grid.ColumnProperty, column);
                    border.SetValue(Grid.RowProperty, row);

                    rootGrid.Children.Add(border);
                }
            }
        }

        private void AddEntryPoint(Dictionary<int, List<SokobanPathItem>> graph, int position, Key key, int neighbor)
        {
            var entryPoint = new SokobanPathItem()
            {
                Position = position,
                Key = key,
                StepsToTarget = 1
            };

            if (!graph.ContainsKey(neighbor))
            {
                graph.Add(neighbor, new List<SokobanPathItem>());
            }

            graph[neighbor].Add(entryPoint);
        }
    }
}
