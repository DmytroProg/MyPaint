using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyPaint
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        private Rectangle selectRect;
        private Canvas chosenColor;

        public ColorPickerWindow()
        {
            InitializeComponent();
            FillColorArea();
        }

        public void ShowDialog(Canvas color)
        {
            chosenColor = color;
            this.ShowDialog();
        }

        private void FillColorArea()
        {
            selectRect = new Rectangle()
            {
                Height = 10,
                Width = 10,
                Stroke = Brushes.Gray,
                StrokeThickness = 3,
                Fill = Brushes.Transparent
            };

            for (int i = 360; i >= 0; i -= 2)
            {
                for (int j = 360; j >= 0; j -= 2)
                {
                    Rectangle r = new Rectangle() { Height = 1, Width = 1, Margin = new Thickness(0), StrokeThickness = 0 };
                    System.Drawing.Color c = ColorFromHSV((double)j, (double)i / 360, (double)i / 360);
                    r.Fill = new SolidColorBrush(Color.FromArgb(255, c.R, c.G, c.B));
                    r.MouseEnter += (sender, e) =>
                    {
                        if(e.LeftButton == MouseButtonState.Pressed)
                        {
                            SolidColorBrush solid = (sender as Rectangle).Fill as SolidColorBrush;
                            var color = System.Drawing.Color.FromArgb(255, solid.Color.R, solid.Color.G, solid.Color.B);
                            hueTextBox.Text = ((int)color.GetHue()).ToString();
                            contrastTextBox.Text = ((int)(color.GetSaturation() * 360)).ToString();
                            SetRGBColumn();
                            SetPickedColor();
                        }
                        
                    };

                    Canvas.SetLeft(r, j/2);
                    Canvas.SetTop(r, (360 - i)/2);
                    colorArea.Children.Add(r);
                }
            }
            Canvas.SetLeft(selectRect, 0);
            Canvas.SetTop(selectRect, 0);
            colorArea.Children.Add(selectRect);
        }

        private void SetPickedColor()
        {
            var color = Color.FromRgb(byte.Parse(redTextBox.Text), byte.Parse(greenTextBox.Text), byte.Parse(blueTextBox.Text));
            pickedColor.Background = new SolidColorBrush(color);
        }

        private System.Drawing.Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return System.Drawing.Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return System.Drawing.Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return System.Drawing.Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return System.Drawing.Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return System.Drawing.Color.FromArgb(255, t, p, v);
            else
                return System.Drawing.Color.FromArgb(255, v, p, q);
        }

        private void brightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brightTextBox.Text = ((int)brightSlider.Value).ToString();
            SetRGBColumn();
            SetPickedColor();
        }

        private void SetHSVColumn()
        {
            var color = System.Drawing.Color.FromArgb(255, int.Parse(redTextBox.Text), int.Parse(greenTextBox.Text), int.Parse(blueTextBox.Text));
            hueTextBox.Text = ((int)color.GetHue()).ToString();
            contrastTextBox.Text = ((int)(color.GetSaturation() * 360)).ToString();
            brightTextBox.Text = ((int)(color.GetBrightness() * 360 * 2)).ToString();
            Canvas.SetLeft(selectRect, Math.Min((int)color.GetHue() / 2, 170));
            Canvas.SetTop(selectRect, Math.Min(180 - ((int)(color.GetSaturation() * 360) / 2), 170));
        }

        private void SetRGBColumn()
        {
            var color = ColorFromHSV(int.Parse(hueTextBox.Text), int.Parse(contrastTextBox.Text)/360.0, int.Parse(brightTextBox.Text)/360.0);
            redTextBox.Text = color.R.ToString();
            greenTextBox.Text = color.G.ToString();
            blueTextBox.Text = color.B.ToString();
            Canvas.SetLeft(selectRect, Math.Min(int.Parse(hueTextBox.Text) / 2, 170));
            Canvas.SetTop(selectRect, Math.Min(180 - (int.Parse(contrastTextBox.Text) / 2), 170));
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Regex.IsMatch(e.Key.ToString(), @"[0-9]"))
                e.Handled = true;
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (String.IsNullOrEmpty((sender as TextBox).Text) || int.Parse((sender as TextBox).Text) > 360)
                (sender as TextBox).Text = "360";
            SetRGBColumn();
            SetPickedColor();
        }

        private void TextBoxRGB_KeyUp(object sender, KeyEventArgs e)
        {
            if (String.IsNullOrEmpty((sender as TextBox).Text) || int.Parse((sender as TextBox).Text) > 255)
                (sender as TextBox).Text = "255";
            SetHSVColumn();
            SetPickedColor();
        }

        private void brightTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox_KeyDown(sender, e);
            brightSlider.Value = int.Parse(brightTextBox.Text);
        }

        private void addColorBtn_Click(object sender, RoutedEventArgs e)
        {
            chosenColor.Background = pickedColor.Background;
            this.Close();
        }
    }
}
