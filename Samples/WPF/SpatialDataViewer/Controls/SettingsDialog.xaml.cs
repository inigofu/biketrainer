using Microsoft.Maps.SpatialToolbox;
using System;
using System.Windows;
using System.Windows.Media;

namespace SpatialDataViewer.Controls
{
    public partial class SettingsDialog : Window
    {
        #region Public Properties

        private ShapeStyle _defaultStyle;

        #endregion

        #region Constructor

        public SettingsDialog()
        {
            InitializeComponent();

            var numbers = new int[20];
            for (int i = 0; i < 20; i++)
            {
                numbers[i] = i + 1;
            }

            StrokeThicknessCbx.ItemsSource = numbers;

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

        public ShapeStyle DefaultStyle
        {
            get
            {
                return _defaultStyle;
            }
            set
            {
                _defaultStyle = value;
                UpdatePanel();
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

        private void FillColorBtn_Clicked(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPickerDialog();

            if (_defaultStyle.FillColor.HasValue)
            {
                colorPicker.SelectedColor = _defaultStyle.FillColor.Value.ToColor();
            }

            colorPicker.Owner = this;
            var r = colorPicker.ShowDialog();

            if (r.HasValue && r.Value)
            {
                if (colorPicker.SelectedColor != null)
                {
                    _defaultStyle.FillColor = colorPicker.SelectedColor.ToStyleColor();
                }
                else
                {
                    _defaultStyle.FillColor = null;
                }

                UpdatePanel();
            }
        }

        private void StrokeColorBtn_Clicked(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPickerDialog();

            if (_defaultStyle.StrokeColor.HasValue)
            {
                colorPicker.SelectedColor = _defaultStyle.StrokeColor.Value.ToColor();
            }

            colorPicker.Owner = this;
            var r = colorPicker.ShowDialog();

            if (r.HasValue && r.Value)
            {
                if (colorPicker.SelectedColor != null)
                {
                    _defaultStyle.StrokeColor = colorPicker.SelectedColor.ToStyleColor();
                }
                else
                {
                    _defaultStyle.StrokeColor = null;
                }

                UpdatePanel();
            }
        }

        private void IconColorBtn_Clicked(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPickerDialog();

            if (_defaultStyle.IconColor.HasValue)
            {
                colorPicker.SelectedColor = _defaultStyle.IconColor.Value.ToColor();
            }

            colorPicker.Owner = this;
            var r = colorPicker.ShowDialog();

            if (r.HasValue && r.Value)
            {
                if (colorPicker.SelectedColor != null)
                {
                    _defaultStyle.IconColor = colorPicker.SelectedColor.ToStyleColor();
                }
                else
                {
                    _defaultStyle.IconColor = null;
                }

                UpdatePanel();
            }
        }

        private void FillPolygonCbx_Clicked(object sender, RoutedEventArgs e)
        {
            _defaultStyle.FillPolygon = (FillPolygonsCbx.IsChecked.HasValue) ? FillPolygonsCbx.IsChecked.Value : true;
        }

        private void OutlinePolygonCbx_Clicked(object sender, RoutedEventArgs e)
        {
            _defaultStyle.OutlinePolygon = (OutlinePolygonsCbx.IsChecked.HasValue) ? OutlinePolygonsCbx.IsChecked.Value : true;
        }

        private void StrokeThicknessCbx_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _defaultStyle.StrokeThickness = (double)StrokeThicknessCbx.SelectedIndex;
        }

        #endregion

        #region Private Properties

        private void UpdatePanel()
        {
            if (_defaultStyle.FillColor.HasValue)
            {
                FillColorBtn.Background = new SolidColorBrush(_defaultStyle.FillColor.Value.ToColor());
            }

            if (_defaultStyle.StrokeColor.HasValue)
            {
                StrokeColorBtn.Background = new SolidColorBrush(_defaultStyle.StrokeColor.Value.ToColor());
            }

            if (_defaultStyle.IconColor.HasValue)
            {
                IconColorBtn.Background = new SolidColorBrush(_defaultStyle.IconColor.Value.ToColor());
            }

            FillPolygonsCbx.IsChecked = _defaultStyle.FillPolygon;
            OutlinePolygonsCbx.IsChecked = _defaultStyle.OutlinePolygon;

            StrokeThicknessCbx.SelectedIndex = (int)Math.Round(_defaultStyle.StrokeThickness);
        }

        #endregion
    }
}
