using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.SpatialToolbox;
using Microsoft.Maps.SpatialToolbox.Bing;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

#if WINDOWS_APP
using Bing.Maps;
using Windows.UI.Xaml;
#elif WPF
using System.Windows.Threading;
using Microsoft.Maps.MapControl.WPF;
#elif WINDOWS_PHONE
using System.Windows.Threading;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
#elif WINDOWS_PHONE_APP
using Windows.UI.Xaml;
using Windows.Devices.Geolocation;
#endif

namespace SpatialDataViewer
{
    /// <summary>
    /// A class for creating animations along a path.
    /// Based on http://blogs.bing.com/maps/2014/09/25/part-2-bring-your-maps-to-life-creating-animations-with-bing-maps-net/
    /// </summary>
    public class DistancePathAnimation
    {
        #region Private Properties

        private const int _delay = 1000;
        private const double EARTH_RADIUS_KM = 6378.1;

        private DispatcherTimer _timerId;

#if WINDOWS_PHONE
        private GeoCoordinateCollection _path;
        private GeoCoordinateCollection _intervalLocs;
#elif WINDOWS_PHONE_APP
        private List<BasicGeoposition> _path;
        private List<BasicGeoposition> _intervalLocs;
#else
        private List<PathPoint> _path;
        private LocationCollection _intervalLocs;
#endif

        private bool _isGeodesic = false;
        private List<int> _intervalIdx;

        private int? _duration;
        private int _frameIdx = 0;
        private bool _isPaused;

        #endregion

        #region Constructor

        /// <summary>This class extends from the BaseAnimation class and cycles through a set of coordinates over a period of time, calculating mid-point coordinates along the way.</summary>
        /// <param name="path">An array of locations to cycle through.</param>
        /// <param name="intervalCallback">A function that is called when a frame is to be rendered. This callback function recieves four values; current cordinate, index on path and frame index.</param>
        /// <param name="isGeodesic">Indicates if the path should follow the curve of the earth.</param>
        /// <param name="duration">Length of time in ms that the animation should run for. Default is 1000 ms.</param>
#if WINDOWS_PHONE
        public PathAnimation(GeoCoordinateCollection path, IntervalCallback intervalCallback, bool isGeodesic, int? duration)
#elif WINDOWS_PHONE_APP
        public PathAnimation(List<BasicGeoposition> path, IntervalCallback intervalCallback, bool isGeodesic, int? duration)
#else
        public DistancePathAnimation(List<PathPoint> path, IntervalCallback intervalCallback, bool isGeodesic, int? duration)
#endif
        {
            _path = path;
            _isGeodesic = isGeodesic;
            _duration = duration;

            

            _timerId = new DispatcherTimer();
            _timerId.Interval = new TimeSpan(0, 0, 0, 0, _delay);
            double _distance=0;

            _timerId.Tick += (s, a) =>
            {
                if (!_isPaused)
                {
                    double progress = (double)(_frameIdx * _delay) / (double)_duration.Value;

                    if (progress > 1)
                    {
                        progress = 1;
                    }

                    if (intervalCallback != null)
                    {
                        List<PathPoint> temppath = path.FindAll(o => o.distance >= _distance);
                        intervalCallback(new Location(temppath[0].latitude, temppath[0].longitude, (double)temppath[0].height), path.Count-temppath.Count, _frameIdx);
                    }

                    if (progress == 1)
                    {
                        _timerId.Stop();
                    }

                    _distance += 100;
                }
            };
        }

        #endregion

        #region Public Delgates

#if WINDOWS_PHONE
        public delegate void IntervalCallback(GeoCoordinate loc, int pathIdx, int frameIdx);
#elif WINDOWS_PHONE_APP
        public delegate void IntervalCallback(BasicGeoposition loc, int pathIdx, int frameIdx);
#else
        public delegate void IntervalCallback(Location loc, int pathIdx, int frameIdx);
#endif

        #endregion

        #region Public Properties

        /// <summary>
        /// The coordinates that make up the path of the animation.
        /// </summary>
#if WINDOWS_PHONE
        public GeoCoordinateCollection Path
#elif WINDOWS_PHONE_APP
        public List<BasicGeoposition> Path
#else
        public List<PathPoint> Path
#endif
        {
            get { return _path; }
            set
            {
                _path = value;
               
            }
        }

        /// <summary>
        /// A boolean indicating if the path should be geodesic (follw curvature of earth).
        /// </summary>
        public bool IsGeodesic
        {
            get { return _isGeodesic; }
            set
            {
                _isGeodesic = value;
                
            }
        }

        /// <summary>
        /// The length of time the animation should take in ms.
        /// </summary>
        public int? Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the animation.
        /// </summary>
        public void Play()
        {
            _frameIdx = 0;
            _isPaused = false;
            _timerId.Start();
        }

        /// <summary>
        /// Pauses the animation.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Stops the animation and resets animation back to first frame.
        /// </summary>
        public void Stop()
        {
            if (_timerId.IsEnabled)
            {
                _frameIdx = 0;
                _isPaused = false;
                _timerId.Stop();
            }
        }

        #endregion

        #region Private Methods

     
        #endregion
    }
}