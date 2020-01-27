using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.SpatialToolbox;
using Microsoft.Maps.SpatialToolbox.Bing;
using Microsoft.Maps.SpatialToolbox.Imaging;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace SpatialDataViewer.Controls
{
    public partial class TileGeneratingDialog : Window
    {
        #region Private Properties

        private SpatialDataSet _dataSet;
        private ShapeStyle _defaultStyle;

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        #region Constructor

        public TileGeneratingDialog(SpatialDataSet dataSet, ShapeStyle defaultStyle)
        {
            InitializeComponent();

            _dataSet = dataSet;
            _defaultStyle = defaultStyle;

            if (_dataSet == null)
            {
                this.Close();
            }

            this.Loaded += (s, e) => {
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

                Application curApp = Application.Current;
                Window mainWindow = curApp.MainWindow;
                this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
                this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;

                //Fill comboboxes.
                var zoomLevels = new int[20];
                for (int i = 0; i < 20; i++)
                {
                    zoomLevels[i] = i + 1;
                }

                MinZoomCbx.ItemsSource = zoomLevels;
                MinZoomCbx.SelectedIndex = 0;

                MaxZoomCbx.ItemsSource = zoomLevels;
                MaxZoomCbx.SelectedIndex = 4;

                TileMsgTbx.Text = string.Format(CultureInfo.InvariantCulture, "Approximately {0:N0} tiles.", CalculateTileCount(_dataSet.BoundingBox.ToBMGeometry(), (int)MinZoomCbx.SelectedValue, (int)MaxZoomCbx.SelectedValue));
                ProgressTbx.Text = "";
                TileProgressBar.Value = 0;
                ProcessTilesBtn.IsEnabled = true;
                CancelBtn.IsEnabled = true;
            };
        }

        #endregion

        #region Event Handlers
        
        private void CancelBtn_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ZoomRangeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var btn = sender as ComboBox;

            if (MinZoomCbx.SelectedValue != null && MaxZoomCbx.SelectedValue != null &&
                (int)MinZoomCbx.SelectedValue > (int)MaxZoomCbx.SelectedValue)
            {
                if (string.Compare(btn.Name, "MinZoomCbx") == 0)
                {
                    MaxZoomCbx.SelectedIndex = MinZoomCbx.SelectedIndex;
                }
                else
                {
                    MinZoomCbx.SelectedIndex = MaxZoomCbx.SelectedIndex;
                }
            }

            if (MinZoomCbx.SelectedValue != null && MaxZoomCbx.SelectedValue != null)
            {
                TileMsgTbx.Text = string.Format(CultureInfo.InvariantCulture, "Approximately {0:N0} tiles.", CalculateTileCount(_dataSet.BoundingBox.ToBMGeometry(), (int)MinZoomCbx.SelectedValue, (int)MaxZoomCbx.SelectedValue));
            }
        }

        private async void ProcessTilesBtn_Clicked(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select an output directory."
            };
            dialog.ShowDialog();

            if (!string.IsNullOrEmpty(dialog.SelectedPath))
            {
                ProcessTilesBtn.IsEnabled = false;
                CancelBtn.IsEnabled = false;

                TileProgressBar.Maximum = CalculateTileCount(_dataSet.BoundingBox.ToBMGeometry(), (int)MinZoomCbx.SelectedValue, (int)MaxZoomCbx.SelectedValue);

                var progressIndicator = new Progress<int>(ReportTileProgress);
                await GenerateTiles(_dataSet, dialog.SelectedPath, (int)MinZoomCbx.SelectedValue, (int)MaxZoomCbx.SelectedValue, RemoveEmptyTileCbx.IsChecked.Value, progressIndicator);
            }

            this.Close();
        }

        private void ReportTileProgress(int tileCount)
        {
            TileProgressBar.Value = tileCount;

            if (tileCount != TileProgressBar.Maximum)
            {
                ProgressTbx.Text = string.Format("{0} of {1} tiles generated.", tileCount, TileProgressBar.Maximum);
            }
            else
            {
                ProgressTbx.Text = "Completed";
            }
        }

        #endregion

        #region Private Properties

        private Task GenerateTiles(SpatialDataSet data, string outputFolderPath, int minZoom, int maxZoom, bool skipEmptyTiles, IProgress<int> progress)
        {
            var strColorName = TileBackgroundColor.SelectedValue.ToString().Replace("System.Windows.Media.Color ", "");
            var backgroundColor = ((Color)ColorConverter.ConvertFromString(strColorName)).ToStyleColor();
            var isQuadkeyFormat = (QuakeyTileFormat as RadioButton).IsChecked.Value;

            return Task.Run(async () =>
            {
                var bounds = data.BoundingBox.ToBMGeometry();
                var renderEngine = new SpatialDataRenderEngine(_defaultStyle, false);
                var tileCount = 0;

                for (int z = minZoom; z <= maxZoom; z++)
                {
                    var keys = TileMath.GetQuadKeysInView(bounds, z);

                    foreach (var key in keys)
                    {
                        await renderEngine.RenderDataAsync(_dataSet, new ViewInfo(256, 256, key), backgroundColor);

                        if (!skipEmptyTiles || !(await renderEngine.IsImageEmpty()))
                        {
                            string filePath;

                            if (isQuadkeyFormat)
                            {
                                filePath = string.Format("{0}/{1}.{2}", outputFolderPath, key.Key, "png");
                            }
                            else
                            {
                                //X Folder Path
                                filePath = outputFolderPath + "/" + key.X;

                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }

                                filePath += "/" + key.Y;

                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }

                                filePath += "/" + key.ZoomLevel + ".png";
                            }

                            using (var fs = File.Create(filePath))
                            {
                                renderEngine.SaveImage(ImageFormat.PNG, fs);
                            }
                        }

                        tileCount++;

                        if (progress != null)
                        {
                            progress.Report(tileCount);
                        }
                    }
                }

                renderEngine.Dispose();

                await Task.Delay(2000);
            });
        }

        /// <summary>
        /// Calcualtes the approximate number of tiles within a bounding box between a set of zoom ranges.
        /// </summary>
        /// <param name="bounds">Bounding box of to focus on.</param>
        /// <param name="minZoom">Minimium zoom level range.</param>
        /// <param name="maxZoom">Maximium zoon level range.</param>
        /// <returns>An approximate number of tiles in this area betwen the specified zoom range.</returns>
        private long CalculateTileCount(LocationRect bounds, int minZoom, int maxZoom)
        {
            long numTiles = 0;
            int tlX, tlY, brX, brY;

            for (int z = minZoom; z <= maxZoom; z++)
            {
                TileMath.LocationToTileXY(bounds.North, bounds.West, z, out tlX, out tlY);
                TileMath.LocationToTileXY(bounds.South, bounds.East, z, out brX, out brY);

                numTiles += (brY - tlY + 1) * (brX - tlX + 1);
            }

            return numTiles;
        }

        #endregion
    }
}
