using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.SpatialToolbox;
using Microsoft.Maps.SpatialToolbox.Bing;
using Microsoft.Maps.SpatialToolbox.Bing.Animations;
using Microsoft.Maps.SpatialToolbox.IO;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SpatialDataViewer.Controls;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpatialDataViewer
{
    public partial class MainWindow : Window
    {
        #region Private Properties

        private ShapeStyle DefaultStyle = new ShapeStyle()
        {
            //FillColor = StyleColor.FromArgb(150, 222, 222, 222)
            FillColor = StyleColor.FromArgb(150, 0, 0, 255),
            StrokeColor = StyleColor.FromArgb(150, 0, 0, 0),
            StrokeThickness = 4
        };

        private SpatialDataSet CurrentDataSet = null;

        private Location mapRightClickPoint;
        private List<PathPoint> path;

        private static System.Timers.Timer aTimer;
        private double distance;
        private Pushpin pin = new Pushpin();
        public PlotModel MyModel { get; private set; }
        #endregion

        #region Constructor

        public MainWindow()
        {
            MyModel = new PlotModel { Title = "Altitude" };
            InitializeComponent();
            path = new List<PathPoint>();
            MyMap.MouseRightButtonUp += (s, e) =>
            {
                mapRightClickPoint = null;
                MyMap.TryViewportPointToLocation(e.GetPosition(MyMap), out mapRightClickPoint);
            };
            this.DataContext = this;
            
        }

        #endregion

        #region Event Handlers

        private void ClearMap_Clicked(object sender, RoutedEventArgs e)
        {
            ClearMap();
        }

        private async void ImportData_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = sender as System.Windows.Controls.MenuItem;

                //Check to see if the Tag property of the selected item has a string that is a spatial file type.
                if (selectedItem != null)
                {
                    string fileType = selectedItem.Tag.ToString();
                    BaseFeed reader = null;
                    string defaultExt = string.Empty;
                    string filters = string.Empty;

                    GetFileSettings(fileType, out reader, out defaultExt, out filters);

                    ClearMap();

                    //Create a FileOpenPicker to allow the user to select which file to import
                    var openPicker = new OpenFileDialog()
                    {
                        DefaultExt = defaultExt,
                        Filter = filters,
                    };

                    //Get the selected file
                    var state = openPicker.ShowDialog();
                    if (state.HasValue && state.Value)
                    {
                        using (var fileStream = openPicker.OpenFile())
                        {
                            SpatialDataSet data = null;
                            if (reader != null)
                            {
                                //Read the spatial data file
                                if (reader is BingDataSourceFeed)
                                {
                                    data = await (reader as BingDataSourceFeed).ReadAsync(fileStream);
                                }
                                else
                                {
                                    data = await reader.ReadAsync(fileStream);
                                }
                            }

                            if (data != null && string.IsNullOrEmpty(data.Error))
                            {
                                //Load the spatial set data into the map
                                MapTools.LoadGeometries(data, PinLayer, PolyLayer, DefaultStyle, GeometryTapped);

                                CurrentDataSet = data;

                                DisplayMetadata(data.Metadata);

                                //If the data set has a bounding box defined, use it to set the map view.
                                if (data.BoundingBox != null)
                                {
                                    MyMap.SetView(data.BoundingBox.ToBMGeometry());
                                }
                                double dist = 0;
                                var  temppath = new LocationCollection();
                                
                                
                                var ls = new LineSeries { Title = "Altitude" };
                                    

                                    

                                for (int i=0;i< ((LineString)((MultiLineString)data.Geometries[0]).Geometries[0]).Vertices.Count; i++){
                                    Coordinate coor = ((LineString)((MultiLineString)data.Geometries[0]).Geometries[0]).Vertices[i];
                                    if (i == 0)
                                    {
                                        path.Add(new PathPoint(coor.Latitude, coor.Longitude, (double)coor.Altitude, 0));
                                    }
                                    else {
                                    Coordinate coorant = ((LineString)((MultiLineString)data.Geometries[0]).Geometries[0]).Vertices[i - 1];
                                        
                                        var sCoord = new GeoCoordinate(coorant.Latitude, coorant.Longitude);
                                        var eCoord = new GeoCoordinate(coor.Latitude, coor.Longitude);
                                        dist += sCoord.GetDistanceTo(eCoord);

                                        path.Add(new PathPoint(coor.Latitude, coor.Longitude, (double)coor.Altitude, dist));
                                        temppath.Add(new Location(coor.Latitude, coor.Longitude));
                                        
                                        double x = dist;
                                        double y = (double)coor.Altitude;
                                        ls.Points.Add(new DataPoint(x, y));
                                       
                                    }
                                }
                                //var pin = new Pushpin();
                                MyModel.Series.Add(ls);
                                var ls2 = new LineSeries { Title = "Real Altitude" };
                                MyModel.Series.Add(ls2);

                                MyModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Altitude" });
                                
                                MyModel.InvalidatePlot(true);
                                Coordinate tmpCoor = ((LineString)((MultiLineString)data.Geometries[0]).Geometries[0]).Vertices[0];
                                MapLayer.SetPosition(pin, new Location(tmpCoor.Latitude, tmpCoor.Longitude, (double)tmpCoor.Altitude));

                                MyMap.Children.Add(pin);
                                DistancePathAnimation currentAnimation = new DistancePathAnimation(path, (coord, pathIdx, frameIdx) =>
                                {
                                    MapLayer.SetPosition(pin, coord);
                                    ((LineSeries)MyModel.Series[1]).Points.Add(new DataPoint(path[pathIdx].distance, path[pathIdx].height));
                                    MyModel.InvalidatePlot(true);
                                }, false, 10000);

                                currentAnimation.Play();

                                //aTimer.Enabled = true;

                            }
                            else
                            {
                                //If there is an error message, display it to the user.
                                MessageBox.Show((data != null) ? data.Error : "Unable to parse file.");
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private async void ExportData_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentDataSet == null)
                {
                    MessageBox.Show("No data to export.");
                    return;
                }

                var selectedItem = sender as System.Windows.Controls.MenuItem;

                BaseFeed writer = null;
                string defaultExt = string.Empty;
                string filters = string.Empty;
                string fileType = selectedItem.Tag.ToString();

                GetFileSettings(fileType, out writer, out defaultExt, out filters);
                
                var sfg = new SaveFileDialog()
                {
                    DefaultExt = defaultExt,
                    Filter = filters
                };

                var state = sfg.ShowDialog();
                if (state.HasValue && state.Value)
                {
                    using (var fileStream = sfg.OpenFile())
                    {
                        if (string.Compare(fileType, "kmz") == 0)
                        {
                            await (writer as KmlFeed).WriteAsync(CurrentDataSet, fileStream, true);
                        }
                        else if (writer is BingDataSourceFeed)
                        {
                            await (writer as BingDataSourceFeed).WriteAsync(CurrentDataSet, fileStream);
                        }
                        else if (writer != null)
                        {
                            await writer.WriteAsync(CurrentDataSet, fileStream);
                        }
                    }

                    MessageBox.Show("Export Complete.");
                }
            }
            catch { }
        }

        private void GenerateTilesBtn_Clicked(object sender, RoutedEventArgs e)
        {
            if (CurrentDataSet == null)
            {
                MessageBox.Show("No data loaded on map.");
                return;
            }
            
            var tileDialog = new TileGeneratingDialog(CurrentDataSet, DefaultStyle);
            tileDialog.Owner = this;
            tileDialog.ShowDialog();
        }

        private void SettingsBtn_Clicked(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsDialog();
            settingsDialog.Owner = this;
            settingsDialog.DefaultStyle = DefaultStyle;

            var r = settingsDialog.ShowDialog();
            if (r.HasValue && r.Value)
            {
                DefaultStyle = settingsDialog.DefaultStyle;
            }
        }

        private void Exit_Clicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Private Methods
        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                              e.SignalTime);
            distance += 1000;
            List<PathPoint> temppath=path.FindAll(o => o.distance >=distance);
            
            MapLayer.SetPosition(pin, new Location(temppath[0].latitude, temppath[0].longitude, (double)temppath[0].height));
            
        }
        private void ClearMap()
        {
            PinLayer.Children.Clear();
            PolyLayer.Children.Clear();
            MetadataTbx.Text = string.Empty;
            MetadataPanel.DataContext = null;
            DescriptionBox.Navigate("about:blank");
            CurrentDataSet = null;
        }

        private void GetFileSettings(string fileType, out BaseFeed feed, out string defaultExt, out string filters)
        {
            switch (fileType)
            {
                case "wkt":
                    feed = new WellKnownText();
                    defaultExt = ".txt";
                    filters = "Text (.txt)|*.txt";
                    break;
                case "shp":
                    feed = new ShapefileReader();
                    defaultExt = ".shp";
                    filters = "Shapefile (.shp)|*.shp";
                    break;
                case "gpx":
                    feed = new GpxFeed();
                    defaultExt = ".gpx";
                    filters = "Xml (.xml)|*.xml|Gpx (.gpx)|*.gpx";
                    break;
                case "georss":
                    feed = new GeoRssFeed();
                    defaultExt = ".xml";
                    filters = "Xml (.xml)|*.xml|Rss (.rss)|*.rss";
                    break;
                case "kml":
                    feed = new KmlFeed();
                    defaultExt = ".kml";
                    filters = "Kml (.kml)|*.kml|Kmz (.kmz)|*.kmz|Xml (.xml)|*.xml";
                    break;
                case "kmz":
                    feed = new KmlFeed();
                    defaultExt = ".kmz";
                    filters = "Kmz (.kmz)|*.kmz|Zip (.zip)|*.zip";
                    break;
                case "geojson":
                    feed = new GeoJsonFeed();
                    defaultExt = ".js";
                    filters = "JavaScript (.js)|*.js|Json (.json)|*.json";
                    break;
                case "bing-xml":
                    feed = new BingDataSourceFeed(BingDataSourceType.XML);
                    defaultExt = ".xml";
                    filters = "Xml (.xml)|*.xml";
                    break;
                case "bing-csv":
                    feed = new BingDataSourceFeed(BingDataSourceType.CSV);                    
                    defaultExt = ".csv";
                    filters = "CSV (*.csv)|*.csv|Text (*.txt)|*.txt|All files (*.*)|*.*";
                    break;
                case "bing-tab":
                    feed = new BingDataSourceFeed(BingDataSourceType.TAB);
                    defaultExt = ".txt";
                    filters = "Text (*.txt)|*.txt|TSV (*.tsv)|*.tsv|All files (*.*)|*.*";
                    break;
                case "bing-pipe":
                    feed = new BingDataSourceFeed(BingDataSourceType.PIPE);
                    defaultExt = ".txt";
                    filters = "Text (*.txt)|*.txt|All files (*.*)|*.*";
                    break;
                default:
                    feed = null;
                    defaultExt = string.Empty;
                    filters = string.Empty;
                    break;
            }
        }

        private void GeometryTapped(object sender, RoutedEventArgs e)
        {
            ShapeMetadata tagMetadata = null;            

            if (sender is MapShapeBase)
            {
                var shape = sender as MapShapeBase;
                
                tagMetadata = shape.Tag as ShapeMetadata;
            }
            else if (sender is System.Windows.UIElement)
            {
                var pin = sender as FrameworkElement;
                //Get the stored metadata from the Tag property
                tagMetadata = pin.Tag as ShapeMetadata;
            }

            DisplayMetadata(tagMetadata);
        }

        private void DisplayMetadata(ShapeMetadata metadata)
        {
            MetadataPanel.DataContext = null;
            DescriptionBox.Navigate("about:blank");

            StringBuilder sb = new StringBuilder();

            if (metadata != null && metadata.HasMetadata())
            {
                MetadataPanel.DataContext = metadata;

                if (!string.IsNullOrEmpty(metadata.Description))
                {
                    DescriptionBox.NavigateToString(metadata.Description);
                }

                foreach (var val in metadata.Properties)
                {
                    sb.AppendLine(val.Key + ": " + val.Value);
                }
            }

            MetadataTbx.Text = sb.ToString();
        }

        #endregion

        private void MapTypeSelected(object sender, RoutedEventArgs e)
        {
            var item = sender as RadioButton;
            var mapType = (string)item.Tag;
            switch (mapType)
            {
                case "aerial":
                    MyMap.Mode = new AerialMode(false);
                    break;
                case "hybrid":
                    MyMap.Mode = new AerialMode(true);
                    break;
                case "road":
                    MyMap.Mode = new RoadMode();
                    break;
            }
        }

        private void CopyCoordinates_Clicked(object sender, RoutedEventArgs e)
        {
            if (mapRightClickPoint != null)
            {
                Clipboard.SetData(DataFormats.Text, string.Format("{0}\t{1}", mapRightClickPoint.Latitude, mapRightClickPoint.Longitude));
            }
        }
    }
}
