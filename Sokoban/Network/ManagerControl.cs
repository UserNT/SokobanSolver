﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sokoban.Network
{
    public class ManagerControl : Control
    {
        private Dictionary<char, Brush> brushes;

        private Grid rootGrid;

        public static readonly DependencyProperty ManagerProperty = DependencyProperty.Register("Manager", typeof(Manager), typeof(ManagerControl), new PropertyMetadata(null, OnManagerPropertyChanged));

        public Manager Manager
        {
            get { return (Manager)GetValue(ManagerProperty); }
            set { SetValue(ManagerProperty, value); }
        }

        private static void OnManagerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as ManagerControl;
            ctrl?.OnManagerPropertyChanged();
        }

        public ManagerControl()
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

            brushes[Solver.Sokoban.BOX] = GetImageBrush("box.gif");
            brushes[Solver.Sokoban.BOX_ON_LOCATION] = GetImageBrush("box_on_location.gif");
            brushes[Solver.Sokoban.KEEPER] = GetImageBrush("keeper.gif");
            brushes[Solver.Sokoban.KEEPER_ON_LOCATION] = GetImageBrush("keeper.gif");
            brushes[Solver.Sokoban.LOCATION] = GetImageBrush("location.gif");
            brushes[Solver.Sokoban.WALL] = GetImageBrush("wall.gif");
        }

        private Brush GetImageBrush(string resourceName)
        {
            return new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Sokoban;component/Resources/" + resourceName)));
        }

        private void OnManagerPropertyChanged()
        {
            if (Manager == null)
            {
                rootGrid.Children.Clear();
                rootGrid.RowDefinitions.Clear();
                rootGrid.ColumnDefinitions.Clear();
                return;
            }

            if (rootGrid.ColumnDefinitions.Count == 0)
            {
                for (int column = 0; column < Manager.Width; column++)
                {
                    rootGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
            }

            if (rootGrid.RowDefinitions.Count == 0)
            {
                for (int row = 0; row < Manager.Height; row++)
                {
                    rootGrid.RowDefinitions.Add(new RowDefinition());
                }
            }

            DrawElements();
        }

        private void DrawElements()
        {
            rootGrid.Children.Clear();

            for (int row = 0; row < Manager.Height; row++)
            {
                for (int column = 0; column < Manager.Width; column++)
                {
                    var pos = Manager.GetPosition(column, row);

                    if (!Manager.StaticGraph.ContainsKey(pos.Value))
                    {
                        continue;
                    }

                    var colorIndex = Manager[pos.Value] == Solver.Sokoban.EMPTY ? (char)(Manager.DynamicGraph[pos.Value].AreaId + 7) : Manager[pos.Value];

                    var border = new Border();
                    border.Background = brushes[colorIndex];

                    CellDynamicInfo dynCellInfo;
                    if (Manager.DynamicGraph.TryGetValue(pos.Value, out dynCellInfo) &&
                        dynCellInfo.StepsToKeeper > 0)
                    {
                        var text = dynCellInfo.StepsToKeeper.ToString();

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