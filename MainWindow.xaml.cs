using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
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
        private bool isSelecting = false;
        private bool isMovingPhoto = false;
        private bool isTexting = false;
        private bool isSaved = true;

        private Point posSelection;
        private string path = null;
        private int format = 0;

        enum BrushType
        {
            None,
            Pencil,
            Bucket,
            Pipette,
            Eraser,
            Text,
            Paint,
            Figure
        }

        enum PaintType
        {
            Paint,
            Marker,
            BigPaint
        }

        private CanvasResizeMode resizeMode;
        private BrushType brushType;
        private PaintType paintType;
        
        private int brushSize = 3;
        private int figureIndex = 0;

        private List<Polyline> polylines;
        private List<Polyline> eraserLines;
        private List<Rectangle> erasedRects;
        private List<Polygon> polygons;

        private Rectangle selectedZone;
        private Image pastedImage;
        private Shape figure;

        public MainWindow()
        {
            InitializeComponent();
            polylines = new List<Polyline>();
            polygons = new List<Polygon>();
            eraserLines = new List<Polyline>();
            erasedRects = new List<Rectangle>();

            SetupValues();

            if (Properties.Settings.Default.Position.Width != 0 && Properties.Settings.Default.Position.Height != 0)
            {
                this.Width = Properties.Settings.Default.Position.Width;
                this.Height = Properties.Settings.Default.Position.Height;
                this.Left = Properties.Settings.Default.Position.X;
                this.Top = Properties.Settings.Default.Position.Y;
                (canvas.Parent as Grid).Width = Properties.Settings.Default.Canvas.X;
                (canvas.Parent as Grid).Height = Properties.Settings.Default.Canvas.Y;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            directionCheckBox.IsChecked = Properties.Settings.Default.Vertical;
            stateCheckBox.IsChecked = Properties.Settings.Default.State;
        }

        private void SetupValues()
        {
            brushType = BrushType.Paint;
            paintType = PaintType.Paint;
            color1Btn.Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
            paintBtn.Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
            whTextBlock.Text = $"{(canvas.Parent as Grid).Width} x {(canvas.Parent as Grid).Height}пкс.";
            selectedZone = new Rectangle();
            stateCheckBox.IsChecked = true;
            foreach (var item in Fonts.SystemFontFamilies)
                fontComboBox.Items.Add(item);
            foreach (var item in new int[]{ 8,9,10,11,12,14,16,18,20,22,24,26,28,36,48,72 }){
                sizeComboBox.Items.Add(item);
            }
            fontComboBox.SelectedIndex = 1;
            sizeComboBox.SelectedIndex = 4;
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
                whTextBlock.Text = $"{(canvas.Parent as Grid).Width} x {(canvas.Parent as Grid).Height}пкс.";
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
            viewPanel.Visibility = Visibility.Hidden;
        }

        private void viewTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewTab.Background = new SolidColorBrush(Color.FromRgb(245, 246, 247));
            mainTab.Background = Brushes.White;
            (viewTab.Parent as Border).BorderBrush = Brushes.Gray;
            (mainTab.Parent as Border).BorderBrush = Brushes.White;

            mainPanel.Visibility = Visibility.Hidden;
            viewPanel.Visibility = Visibility.Visible;
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
            ((sender as Image).Parent as Grid).MouseLeave -= Panel_MouseLeave;
        }

        private void LoseSelection()
        {
            foreach(var item in elementsGrid.Children)
            {
                (item as Grid).Background = Brushes.Transparent;
                (item as Grid).MouseLeave -= Panel_MouseLeave;
                (item as Grid).MouseLeave += Panel_MouseLeave;
            }
            paintBtn.Background = Brushes.Transparent;

            foreach(var item in figuresPanel.Children)
            {
                (item as Grid).Background = Brushes.Transparent;
                (item as Grid).MouseLeave -= Panel_MouseLeave;
                (item as Grid).MouseLeave += Panel_MouseLeave;
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMovingPhoto) return;
            if (isSelecting)
            {
                selectedZone = new Rectangle() { Stroke = Brushes.Blue, StrokeDashArray = new DoubleCollection() { 3, 3 },
                                                 Fill = Brushes.Transparent };
                posSelection = e.GetPosition(canvas);
                Canvas.SetLeft(selectedZone, posSelection.X);
                Canvas.SetTop(selectedZone, posSelection.Y);
                
                canvas.Children.Add(selectedZone);
                return;
            }
            if (brushType == BrushType.None) return;
            if(brushType == BrushType.Bucket)
            {
                canvas.Background = color1.Background;
                foreach (var line in eraserLines)
                    line.Stroke = color1.Background;
                foreach (var r in erasedRects)
                    r.Fill = color1.Background;
                return;
            }
            if(brushType == BrushType.Figure)
            {
                switch (figureIndex)
                {
                    case 1: figure = new Rectangle(); break;
                    case 2: figure = new Ellipse(); break;
                    case 3: figure = new Polyline(); break;
                }
                figure.Stroke = color1.Background;
                figure.StrokeThickness = brushSize;
                figure.Fill = Brushes.Transparent;

                Canvas.SetTop(figure, e.GetPosition(canvas).Y);
                Canvas.SetLeft(figure, e.GetPosition(canvas).X);
                canvas.Children.Add(figure);
                return;
            }
            if(brushType == BrushType.Text && !isTexting)
            {
                TextBox tb = new TextBox() { 
                    FontSize = double.Parse(sizeComboBox.SelectedValue.ToString()),
                    FontFamily = new FontFamily(fontComboBox.SelectedValue.ToString()),
                    Background = Brushes.Transparent,
                    Foreground = color1.Background,
                    FontWeight = boldComboBox.IsChecked == true? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = italicComboBox.IsChecked == true? FontStyles.Italic : FontStyles.Normal,
                };
                Canvas.SetLeft(tb, e.GetPosition(canvas).X);
                Canvas.SetTop(tb, e.GetPosition(canvas).Y);
                canvas.Children.Add(tb);
                isTexting = true;
                tb.Focus();
                return;
            }
            if (brushType == BrushType.Text && isTexting)
            {
                var tBox = (canvas.Children[canvas.Children.Count - 1] as TextBox);
                TextBlock tb = new TextBlock()
                {
                    FontFamily = tBox.FontFamily,
                    FontWeight = tBox.FontWeight,
                    FontStyle = tBox.FontStyle,
                    FontSize = tBox.FontSize,
                    Background = Brushes.Transparent,
                    Foreground = tBox.Foreground,
                    Text = tBox.Text,
                };
                Canvas.SetLeft(tb, Canvas.GetLeft(tBox));
                Canvas.SetTop(tb, Canvas.GetTop(tBox));
                canvas.Children.Remove(tBox);
                canvas.Children.Add(tb);
                isTexting = false;
            }
            if (brushType == BrushType.Pipette)
            {
                ((isFirstColor) ? color1 : color2).Background = canvas.Background;
                return;
            }
            isDrawing = true;

            SetPainting(e);
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            positionTextBlock.Text = $"{e.GetPosition(canvas).X}, {e.GetPosition(canvas).Y}пкс.";
            if (isSelecting)
            {
                var point = e.GetPosition(canvas);
                selectedZone.Height = Math.Max(0, point.Y - posSelection.Y);
                selectedZone.Width = Math.Max(0, point.X - posSelection.X);
            }
            if(brushType == BrushType.Figure && figure != null)
            {
                if (figure is Polyline)
                {
                    (figure as Polyline).Points = new PointCollection() { 
                        new Point(0, 0),
                        new Point(e.GetPosition(canvas).X - Canvas.GetLeft(figure), 
                        e.GetPosition(canvas).Y - Canvas.GetTop(figure))
                    };
                }
                else
                {
                    figure.Height = Math.Abs(Canvas.GetTop(figure) - e.GetPosition(canvas).Y);
                    figure.Width = Math.Abs(Canvas.GetLeft(figure) - e.GetPosition(canvas).X);
                }
            }
            else if (isDrawing)
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
                        if(brushType == BrushType.Pipette)
                        {
                            ((isFirstColor) ? color1 : color2).Background = (s as Polygon).Fill;
                            e1.Handled = true;
                        }
                    };
                    polygons.Add(polygon);
                    canvas.Children.Add(polygon);

                    SetPainting(e);
                }
                
                polylines.Last().Points.Add(point);
            }
        }

        private void SetPainting(MouseEventArgs e)
        {
            var polyline = new Polyline();
            polyline.StrokeThickness = 1;
            polyline.Stroke = color1.Background;
            polylines.Add(polyline);
            polyline.Points.Add(e.GetPosition(canvas));
            canvas.Children.Add(polyline);

            if (brushType == BrushType.Paint)
            {
                polyline.StrokeThickness = brushSize;
                polyline.Stroke = color1.Background;
                switch (paintType)
                {
                    case PaintType.Marker:
                        polyline.Opacity = 0.5;
                        break;
                    case PaintType.BigPaint:
                        polyline.Stroke = new LinearGradientBrush() {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(1, 0),
                            GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(){ Color = Colors.Transparent, Offset = 0},
                                new GradientStop(){ Color = (color1.Background as SolidColorBrush).Color, Offset = 0.2},
                                new GradientStop(){ Color = (color1.Background as SolidColorBrush).Color, Offset = 1}
                            }
                        };
                        break;
                }
            }
            else if (brushType == BrushType.Pencil)
            {
                polyline.StrokeThickness = 1;
                polyline.Stroke = color1.Background;
            }
            else if (brushType == BrushType.Eraser)
            {
                polyline.StrokeThickness = brushSize;
                polyline.Stroke = color2.Background;
            }
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(pastedImage != null)
            {
                (pastedImage.Parent as Border).BorderThickness = new Thickness(0);
                isMovingPhoto = false;
                isDrawing = true;
                (pastedImage.Parent as Border).MouseMove -= pastedPhoto_MouseMove;
                (pastedImage.Parent as Border).MouseUp -= pastedPhoto_MouseUp;
                pastedImage = null;
            }
            if(isSelecting == false && canvas.Children.Contains(selectedZone))
                canvas.Children.Remove(selectedZone);
            if(brushType == BrushType.Figure)
            {
                figure.MouseLeftButtonDown += (s, e1) =>
                {
                    (s as Shape).Fill = color1.Background;
                    e1.Handled = true;
                };
                figure = null;
                brushType = BrushType.None;
            }
            if(brushType == BrushType.Text && isTexting)
            {
                (canvas.Children[canvas.Children.Count - 1] as TextBox).Focus();
            }
            isSelecting = false;
            isDrawing = false;
            if (brushType == BrushType.Eraser)
            {
                eraserLines.Add(polylines.Last());
                polylines.Remove(polylines.Last());
            }

            if (brushType != BrushType.None)
                isSaved = false;
            this.Cursor = Cursors.Arrow;
        }

        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private void textElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Text;
            SelectBackground(sender);
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

        private void Panel_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Panel).Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
        }

        private void Panel_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Panel).Background = Brushes.Transparent;
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

        private void diffColorBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ColorPickerWindow colorPickerWindow = new ColorPickerWindow();
            colorPickerWindow.ShowDialog((isFirstColor)? color1 : color2);
        }

        private void pipette_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Pipette;
            SelectBackground(sender);
        }

        private void eraser_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Eraser;
            SelectBackground(sender);
        }

        private void paint3DBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    Arguments = @"/c mspaint /ForceBootstrapPaint3D",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "cmd"
                });
            }
            catch
            {
                MessageBox.Show("Здається на вашому комп'ютері немає Paint 3D", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SizeGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            brushSize = (int)((sender as Grid).Children[0] as Rectangle).Height;

            sizePopup.IsOpen = false;
            e.Handled = true;
        }

        private void sizeBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            sizePopup.IsOpen = true;
            foreach (var child in sizeStackPanel.Children)
            {
                (child as Grid).MouseLeave -= Panel_MouseLeave;
                if ((int)((child as Grid).Children[0] as Rectangle).Height == brushSize)
                {
                    Panel_MouseEnter((child as Grid), null);
                }
                else
                {
                    Panel_MouseLeave((child as Grid), null);
                    (child as Grid).MouseLeave += Panel_MouseLeave;
                }
            }
        }

        private void paintBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Paint;
            LoseSelection();
            paintBtn.Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
        }

        private void paintsStackPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            paintPopup.IsOpen = true;
        }

        private void ChoosePaint(object sender)
        {
            foreach (var elem in paintWrapPanel.Children)
            {
                Panel_MouseLeave((elem as Grid), null);
            }
            (sender as Grid).Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
            paintImg.Source = ((sender as Grid).Children[0] as Image).Source;
            paintPopup.IsOpen = false;
        }

        private void paintElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            paintType = PaintType.Paint;
            ChoosePaint(sender);
        }

        private void markerElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            paintType = PaintType.Marker;
            ChoosePaint(sender);
        }

        private void bigPaintElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            paintType = PaintType.BigPaint;
            ChoosePaint(sender);
        }

        private void Clear_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (canvas.Children.Contains(selectedZone))
            {
                Rectangle tempR = new Rectangle()
                {
                    Height = selectedZone.Height,
                    Width = selectedZone.Width,
                    Fill = canvas.Background
                };
                Canvas.SetLeft(tempR, posSelection.X);
                Canvas.SetTop(tempR, posSelection.Y);
                canvas.Children.Add(tempR);
                erasedRects.Add(tempR);
                canvas.Children.Remove(selectedZone);
            }
            else 
                canvas.Children.Clear();
        }

        private void pasteImg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var source = Clipboard.GetImage();
                var image = new Image() { Source = source, Stretch = Stretch.Uniform };
                Border border = new Border()
                {
                    Child = image,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Blue
                };
                
                pastedImage = image;
                isMovingPhoto = true;
                isDrawing = false;
                border.MouseMove += pastedPhoto_MouseMove;

                border.MouseUp += pastedPhoto_MouseUp;

                Canvas.SetTop(border, 0);
                Canvas.SetLeft(border, 0);
                canvas.Children.Add(border);
            }
        }

        private void pastedPhoto_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Canvas.SetTop((sender as Border), e.GetPosition(canvas).Y - 10);
                Canvas.SetLeft((sender as Border), e.GetPosition(canvas).X - 10);
            }
        }

        private void pastedPhoto_MouseUp(object sender, MouseEventArgs e)
        {
            isMovingPhoto = false;
            pastedImage = null;
            (sender as Border).BorderThickness = new Thickness(0);
            isDrawing = true;
            (sender as Border).MouseMove -= pastedPhoto_MouseMove;
            (sender as Border).MouseUp -= pastedPhoto_MouseUp;
        }

        private void selectBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isSelecting = true;
        }

        #region PNG_Work
        public static void RenderToPNGFile(Visual targetControl, string filename)
        {
            var renderTargetBitmap = GetRenderTargetBitmapFromControl(targetControl);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            var result = new BitmapImage();

            try
            {
                using (var fileStream = new FileStream(filename, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"There was an error saving the file: {ex.Message}");
            }
        }

        private const double defaultDpi = 96.0;

        private static BitmapSource GetRenderTargetBitmapFromControl(Visual targetControl, double dpi = defaultDpi)
        {
            if (targetControl == null) return null;

            var bounds = VisualTreeHelper.GetDescendantBounds(targetControl);
            var renderTargetBitmap = new RenderTargetBitmap((int)(bounds.Width * dpi / 96.0),
                                                            (int)(bounds.Height * dpi / 96.0),
                                                            dpi,
                                                            dpi,
                                                            PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(targetControl);
                drawingContext.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
            }

            renderTargetBitmap.Render(drawingVisual);
            return renderTargetBitmap;
        }
        #endregion

        private void saveasBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var brush = canvas.Background;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG file (*.png)|*.png|JPEG file (*.jpg)|*.jpg";
            bool? result = saveFileDialog.ShowDialog();

            if(saveFileDialog.FilterIndex == 1)
                canvas.Background = Brushes.Transparent;

            this.UpdateLayout();

            if (result == true)
            {  
                RenderToPNGFile(canvas, saveFileDialog.FileName);
                path = saveFileDialog.FileName;
                format = saveFileDialog.FilterIndex;
                isSaved = true;
            }
            
            canvas.Background = brush;
            
            filePopup.IsOpen = false;

            if(e != null)
                e.Handled = true;
        }

        private void fileTab_MouseUp(object sender, MouseButtonEventArgs e)
        {
            filePopup.IsOpen = true;
        }

        private void saveBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            filePopup.IsOpen = false;
            if (isSaved) return;
            if (String.IsNullOrEmpty(path))
            {
                saveasBtn_MouseUp(null, e);
            }
            else
            {
                var brush = canvas.Background;

                if (format == 1)
                    canvas.Background = Brushes.Transparent;

                this.UpdateLayout();

                RenderToPNGFile(canvas, path);
                isSaved = true;

                canvas.Background = brush;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Position = this.RestoreBounds;
            Properties.Settings.Default.Canvas =
                new System.Drawing.Point((int)(canvas.Parent as Grid).Width, (int)(canvas.Parent as Grid).Height);
            Properties.Settings.Default.Vertical = directionCheckBox.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.State = stateCheckBox.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.Save();

            if (isSaved) return;
            MessageBoxResult result = MessageBox.Show("Файл не збережено. Бажаєте зберегти зміни перед закриттям?",
                "MyPaint", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if(result == MessageBoxResult.Yes)
            {
                saveBtn_MouseUp(null, null);
                if(!isSaved)
                    e.Cancel = true;
            }
            else if(result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void createBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            filePopup.IsOpen = false;

            if (!isSaved)
            {
                MessageBoxResult result = MessageBox.Show("Файл не збережено. Бажаєте зберегти зміни перед закриттям?",
                    "MyPaint", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    saveBtn_MouseUp(null, null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    filePopup.IsOpen = false;
                    return;
                }
            }

            Clear_MouseUp(null, null);
            path = null;
            format = 0;
            isSaved = true;
        }

        private void directionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int tempHeight = (int)(canvas.Parent as Grid).Height;
            (canvas.Parent as Grid).Height = (canvas.Parent as Grid).Width;
            (canvas.Parent as Grid).Width = tempHeight;
        }

        private void stateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            (this.Content as Grid).RowDefinitions.Last().Height = 
                (stateCheckBox.IsChecked == true) ? new GridLength(20) : new GridLength(0);
        }

        private void maxBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void ChooseFigure(object sender)
        {
            LoseSelection();
            (sender as Grid).Background = new SolidColorBrush(Color.FromRgb(201, 224, 247));
            (sender as Grid).MouseLeave -= Panel_MouseLeave;
        }

        private void square_MouseUp(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Figure;
            figureIndex = 1;
            ChooseFigure(sender);
        }

        private void circle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Figure;
            figureIndex = 2;
            ChooseFigure(sender);
        }

        private void line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            brushType = BrushType.Figure;
            figureIndex = 3;
            ChooseFigure(sender);
        }
    }
}
