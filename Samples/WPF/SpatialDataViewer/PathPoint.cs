using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialDataViewer
{
    public class PathPoint
    {
        public double latitude;
        public double longitude;
        public double height;
        public double distance;
        public PathPoint(double m_latitude, double m_longitude, double m_height, double m_distance)
        {
            latitude = m_latitude;
            longitude = m_longitude;
            height = m_height;
            distance = m_distance;
        }
    }
}
