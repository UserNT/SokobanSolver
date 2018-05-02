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
    public class ColorMapControl : Control
    {
        private const double TouchPointBorder = 6;
        private static readonly Brush AccessableTouchPointBorderBrush = new SolidColorBrush(Colors.Lime);
        private static readonly Brush InaccessableTouchPointBorderBrush = new SolidColorBrush(Colors.Red);
        private static readonly Brush LocationTouchPointBorderBrush = new SolidColorBrush(Colors.Blue);

        private Dictionary<char, Brush> brushes;

        private Grid rootGrid;

        public static readonly DependencyProperty ColorMapProperty = DependencyProperty.Register("ColorMap", typeof(ColorMap), typeof(ColorMapControl), new PropertyMetadata(null, OnColorMapPropertyChanged));

        public ColorMap ColorMap
        {
            get { return (ColorMap)GetValue(ColorMapProperty); }
            set { SetValue(ColorMapProperty, value); }
        }

        private static void OnColorMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as ColorMapControl;
            ctrl?.OnColorMapPropertyChanged();
        }

        public ColorMapControl()
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

            brushes[ColorMap.BOX] = GetImageBrush("box.gif");
            brushes[ColorMap.BOX_ON_LOCATION] = GetImageBrush("box_on_location.gif");
            brushes[ColorMap.KEEPER] = GetImageBrush("keeper.gif");
            brushes[ColorMap.KEEPER_ON_LOCATION] = GetImageBrush("keeper.gif");
            brushes[ColorMap.LOCATION] = GetImageBrush("location.gif");
            brushes[ColorMap.WALL] = GetImageBrush("wall.gif");
            brushes[Convert.ToChar(ColorMap.LocationColorIndex)] = GetImageBrush("location.gif");
            brushes[Convert.ToChar(ColorMap.KeeperColorIndex)] = GetImageBrush("keeper.gif");
            brushes[Convert.ToChar(ColorMap.KeeperOnLocationColorIndex)] = GetImageBrush("keeper.gif");
        }

        private Brush GetImageBrush(string resourceName)
        {
            return new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Sokoban;component/Resources/" + resourceName)));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            ColorMap newColorMap;
            if (ColorMap != null &&
                ColorMap.TryMoveKeeper(e.Key, out newColorMap))
            {
                ColorMap = newColorMap;
                e.Handled = true;
            }
        }

        private void OnColorMapPropertyChanged()
        {
            if (ColorMap == null)
            {
                rootGrid.Children.Clear();
                rootGrid.RowDefinitions.Clear();
                rootGrid.ColumnDefinitions.Clear();
                return;
            }

            if (rootGrid.ColumnDefinitions.Count == 0)
            {
                for (int column = 0; column < ColorMap.Width; column++)
                {
                    rootGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
            }

            if (rootGrid.RowDefinitions.Count == 0)
            {
                for (int row = 0; row < ColorMap.Height; row++)
                {
                    rootGrid.RowDefinitions.Add(new RowDefinition());
                }
            }

            DrawElements();

            if (ColorMap.IsComplete())
            {
                MessageBox.Show("Done");
            }
        }

        private void DrawElements()
        {
            rootGrid.Children.Clear();

            var map = ColorMap.ToString();
            var fillingOrder = ColorMap.LocationFillingOrder.OrderByDescending(x => x.Value.Count).First().Value;

            for (int row = 0; row < ColorMap.Height; row++)
            {
                for (int column = 0; column < ColorMap.Width; column++)
                {
                    var pos = ColorMap.GetPosition(column, row);
                    var colorIndex = map[pos];

                    var border = new Border();
                    border.Background = brushes[colorIndex];
                    border.SetValue(Grid.ColumnProperty, column);
                    border.SetValue(Grid.RowProperty, row);
                    
                    foreach (var touchPoint in ColorMap.ColorToBoxTouchPoints
                                                       .SelectMany(pair => pair.Value)
                                                       .Where(tp => tp.Position == pos))
                    {
                        bool isAccessible = map[touchPoint.Position] == ColorMap.KeeperColorIndex || ColorMap.ColorToKeeperTouchPoint.ContainsKey(map[touchPoint.Position]);

                        border.BorderBrush = isAccessible ? AccessableTouchPointBorderBrush : InaccessableTouchPointBorderBrush;
                        ApplyTouchPointThickness(touchPoint, border);
                    }
                    
                    foreach (var touchPoint in ColorMap.ColorToLocationTouchPoints
                                                       .SelectMany(pair => pair.Value)
                                                       .Where(tp => tp.Position == pos))
                    {
                        border.BorderBrush = LocationTouchPointBorderBrush;
                        ApplyTouchPointThickness(touchPoint, border);
                    }

                    var locationFillingIndex = fillingOrder.IndexOf(pos);
                    if (locationFillingIndex != -1)
                    {
                        var viewBox = new Viewbox();
                        viewBox.Stretch = Stretch.Uniform;
                        viewBox.Child = new TextBlock() { Text = locationFillingIndex.ToString(), Foreground = new SolidColorBrush(Colors.Black) };
                        border.Child = viewBox;
                    }

                    rootGrid.Children.Add(border);
                }
            }

            if (ColorMap.ColorToKeeperTouchPoint.Count > 1)
            {
                rootGrid.Background = brushes[ColorMap.ColorToKeeperTouchPoint.Where(pair => pair.Key != ColorMap.LocationColorIndex).First().Key];
            }
            else if (ColorMap.ColorToKeeperTouchPoint.Count == 1)
            {
                rootGrid.Background = brushes[ColorMap.ColorToKeeperTouchPoint.First().Key];
            }
            else
            {
                rootGrid.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void ApplyTouchPointThickness(SokobanPathItem touchPoint, Border border)
        {
            var thickness = border.BorderThickness;

            if (touchPoint.Key == Key.Left)
            {
                thickness.Left = TouchPointBorder;
            }
            else if (touchPoint.Key == Key.Up)
            {
                thickness.Top = TouchPointBorder;
            }
            else if (touchPoint.Key == Key.Right)
            {
                thickness.Right = TouchPointBorder;
            }
            else if (touchPoint.Key == Key.Down)
            {
                thickness.Bottom = TouchPointBorder;
            }

            border.BorderThickness = thickness;
        }
    }
}
