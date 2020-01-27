using System;
using System.Windows;
using System.Windows.Media;

namespace SpatialDataViewer.Controls
{
    public partial class ColorPickerDialog : Window
    {
        #region Private Properties

        private Color _selectedColor;

        #endregion

        #region Constructor

        public ColorPickerDialog()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                Application curApp = Application.Current;
                Window mainWindow = curApp.MainWindow;
                this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
                this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;
            };
        }

        #endregion

        #region Public Properties

        public Color SelectedColor
        {
            get
            {
                return _selectedColor;
            }
            set
            {
                _selectedColor = value;

                ColorPreviewPanel.Background = new SolidColorBrush(_selectedColor);
                aSlider.Value = _selectedColor.A;
                rSlider.Value = _selectedColor.R;
                gSlider.Value = _selectedColor.G;
                bSlider.Value = _selectedColor.B;
            }
        }

        #endregion

        #region Event Handlers

        private void OkBtn_Clicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelBtn_Clicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ColorSlide_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as System.Windows.Controls.Slider;

            switch (slider.Name)
            {
                case "aSlider":
                    _selectedColor.A = (byte)Math.Round(aSlider.Value);
                    break;
                case "rSlider":
                    _selectedColor.R = (byte)Math.Round(rSlider.Value);
                    break;
                case "gSlider":
                    _selectedColor.G = (byte)Math.Round(gSlider.Value);
                    break;
                case "bSlider":
                    _selectedColor.B = (byte)Math.Round(bSlider.Value);
                    break;
            }

            ColorPreviewPanel.Background = new SolidColorBrush(_selectedColor);
        }

        #endregion
    }
}
