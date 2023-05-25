using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        enum CanvasResizeMode
        {
            Right,
            Bottom,
            RightBottom
        }
        private bool isResize = false;
        private bool isDrawing = false;
        private bool isFirstColor = true;

        enum BrushType
        {
            None,
            Pencil,
            Bucket,
            Text,
            Paint
        }

        private CanvasResizeMode resizeMode;
        private BrushType brushType;

        private List<Polyline> polylines;
        private List<Polygon> polygons;

        public MainWindow()
        {
            InitializeComponent();
            polylines = new List<Polyline>();
            polygons = new List<Polygon>();
            brushType = BrushType.None;
            color1Btn.Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
        }

        private void RightRectMouseDown(object sender, MouseEventArgs e)
        {
            resizeMode = CanvasResizeMode.Right;
            isResize = true;
        }

        private void BottomRectMouseDown(object sender, MouseEventArgs e)
        {
            resizeMode = CanvasResizeMode.Bottom;
            isResize = true;
        }

        private void RightBottomRectMouseDown(object sender, MouseEventArgs e)
        {
            resizeMode = CanvasResizeMode.RightBottom;
            isResize = true;
        }

        private void RectMouseMove(object sender, MouseEventArgs e)
        {
            if (isResize)
            {
                switch (resizeMode)
                {
                    case CanvasResizeMode.Right:
                        (canvas.Parent as Grid).Width = Math.Max(16, e.GetPosition(this).X - 5);
                        this.Cursor = Cursors.SizeWE;
                        break;
                    case CanvasResizeMode.Bottom:
                        (canvas.Parent as Grid).Height = Math.Max(16, e.GetPosition(this).Y - 115);
                        this.Cursor = Cursors.SizeNS;
                        break;
                    case CanvasResizeMode.RightBottom:
                        (canvas.Parent as Grid).Width = Math.Max(16, e.GetPosition(this).X - 5);
                        (canvas.Parent as Grid).Height = Math.Max(16, e.GetPosition(this).Y - 115);
                        this.Cursor = Cursors.SizeNWSE;
                        break;
                }
            }
        }

        private void ScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isResize = false;
            this.Cursor = Cursors.Arrow;
        }

        private void mainTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainTab.Background = new SolidColorBrush(Color.FromRgb(245, 246, 247));
            viewTab.Background = Brushes.White;
            (mainTab.Parent as Border).BorderBrush = Brushes.Gray;
            (viewTab.Parent as Border).BorderBrush = Brushes.White;

            mainPanel.Visibility = Visibility.Visible;
        }

        private void viewTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewTab.Background = new SolidColorBrush(Color.FromRgb(245, 246, 247));
            mainTab.Background = Brushes.White;
            (viewTab.Parent as Border).BorderBrush = Brushes.Gray;
            (mainTab.Parent as Border).BorderBrush = Brushes.White;

            mainPanel.Visibility = Visibility.Hidden;
        }

        private void fileTab_MouseEnter(object sender, MouseEventArgs e)
        {
            fileTab.Background = new SolidColorBrush(Color.FromRgb(41, 140, 255));
        }

        private void fileTab_MouseLeave(object sender, MouseEventArgs e)
        {
            fileTab.Background = new SolidColorBrush(Color.FromRgb(25, 121, 202));
        }

        private void pencil_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Pencil;
            SelectBackground(sender);
        }
        
        private void bucket_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Bucket;
            SelectBackground(sender);
        }

        private void SelectBackground(object sender)
        {
            LoseSelection();
            ((sender as Image).Parent as Grid).Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
        }

        private void LoseSelection()
        {
            foreach(var item in elementsGrid.Children)
            {
                (item as Grid).Background = Brushes.Transparent;
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (brushType == BrushType.None) return;
            if(brushType == BrushType.Bucket)
            {
                canvas.Background = color1.Background;
                return;
            }
            if(brushType == BrushType.Text)
            {
               
            }
            isDrawing = true;
            switch (brushType)
            {
                case BrushType.Pencil:
                    var polyline = new Polyline();
                    polyline.StrokeThickness = 1;
                    polyline.Stroke = color1.Background;
                    polylines.Add(polyline);
                    polyline.Points.Add(e.GetPosition(canvas));
                    canvas.Children.Add(polyline);
                    break;
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                if (brushType == BrushType.Bucket) return;
                if (polylines.Last().Points.Last() == e.GetPosition(canvas)) 
                    return;
                
                var point = e.GetPosition(canvas);

                if (polylines.Last().Points.Count > 0 && polylines.Last().Points.Contains(point))
                {
                    var polygon = new Polygon();
                    polygon.Points = polylines.Last().Points;
                    polygon.Fill = Brushes.Transparent;
                    polygon.MouseDown += (s, e1) =>
                    {
                        if (brushType == BrushType.Bucket)
                        {
                            (s as Polygon).Fill = color1.Background;
                            e1.Handled = true;
                        }
                    };
                    polygons.Add(polygon);
                    canvas.Children.Add(polygon);

                    var polyline = new Polyline();
                    polyline.StrokeThickness = 1;
                    polyline.Stroke = color1.Background;
                    polylines.Add(polyline);
                    polyline.Points.Add(e.GetPosition(canvas));
                    canvas.Children.Add(polyline);
                }
                
                polylines.Last().Points.Add(point);
            }
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
        }

        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private void textElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Text;
        }

        private void color1Btn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isFirstColor = true;
            colorBtn_MouseEnter(color1Btn, e);
            colorBtn_MouseLeave(color2Btn, e);
        }

        private void color2Btn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isFirstColor = false;
            colorBtn_MouseEnter(color2Btn, e);
            colorBtn_MouseLeave(color1Btn, e);
        }

        private void colorBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as StackPanel).Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
        }

        private void colorBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isFirstColor && (sender as StackPanel) == color1Btn) return;
            if (!isFirstColor && (sender as StackPanel) == color2Btn) return;
            (sender as StackPanel).Background = Brushes.Transparent;
        }

        private void ColorPicker_MouseUp(object sender, MouseEventArgs e)
        {
            if (isFirstColor)
                color1.Background = ((sender as Border).Child as Canvas).Background;
            else color2.Background = ((sender as Border).Child as Canvas).Background;
        }
    }
}
