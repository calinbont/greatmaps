
namespace GMap.NET.MapProviders
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;
    using GMap.NET.Internals;
    using GMap.NET.Projections;

    public abstract class HereMapProviderBase : GMapProvider, GeocodingProvider, RoutingProvider
    {
        public HereMapProviderBase()
        {
            MaxZoom = null;
            RefererUrl = "http://wego.here.com/";
            Copyright = string.Format("©{0} Here - Map data ©{0} NAVTEQ, Imagery ©{0} DigitalGlobe", DateTime.Today.Year);
        }

        public string AppId = string.Empty;
        public string AppCode = string.Empty;
        protected static readonly string UrlServerLetters = "1234";

        #region GMapProvider Members
        public override Guid Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override PureProjection Projection
        {
            get
            {
                return MercatorProjection.Instance;
            }
        }

        GMapProvider[] overlays;
        public override GMapProvider[] Overlays
        {
            get
            {
                if (overlays == null)
                {
                    overlays = new GMapProvider[] { this };
                }
                return overlays;
            }
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region GMapRoutingProvider Members

        public MapRoute GetRoute(PointLatLng start, PointLatLng end, bool avoidHighways, bool walkingMode, int Zoom)
        {
            List<PointLatLng> points = GetRoutePoints(MakeRoutingUrl(start, end, avoidHighways ? ShortestStr : FastestStr, walkingMode ? TruckStr : CarStr));
            MapRoute route = points != null ? new MapRoute(points, walkingMode ? TruckStr : CarStr) : null;
            return route;
        }

        /// <summary>
        /// NotImplemented
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="avoidHighways"></param>
        /// <param name="walkingMode"></param>
        /// <param name="Zoom"></param>
        /// <returns></returns>
        /// RoutingMode = Type + [TransportModes] + [TrafficMode] + [Feature]

        ///RoutingTypeType [fastest | shortest | balanced];
        ///[TransportModes] [car | pedestrian | carHOV | publicTransport | publicTransportTimeTable | truck | bicycle];

        public MapRoute GetRoute(string start, string end, bool avoidHighways, bool walkingMode, int Zoom)
        {
            throw new NotImplementedException("use GetRoute(PointLatLng start, PointLatLng end...");
        }

        #region -- internals --
        string MakeRoutingUrl(PointLatLng start, PointLatLng end, string travelType, string VehicleType)
        {
            return string.Format(CultureInfo.InvariantCulture, RoutingUrlFormat, start.Lat, start.Lng, end.Lat, end.Lng, travelType, VehicleType, AppId, AppCode);
        }

        List<PointLatLng> GetRoutePoints(string url)
        {
            List<PointLatLng> points = null;
            try
            {
                string route = GMaps.Instance.UseRouteCache ? Cache.Instance.GetContent(url, CacheType.RouteCache) : string.Empty;
                bool cache = false;
                if (string.IsNullOrEmpty(route))
                {
                    route = GetContentUsingHttp(url);
                    if (!string.IsNullOrEmpty(route)) { cache = true; };
                }

                if (!string.IsNullOrEmpty(route))
                {
                    if (route.StartsWith("<?xml") && route.Contains("CalculateRoute") && route.Contains("<Response") && route.Contains("<Route"))
                    {
                        if (cache && GMaps.Instance.UseRouteCache)
                        {
                            Cache.Instance.SaveContent(url, CacheType.RouteCache, route);
                        }

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(route);
                        {
                            XmlNode r = doc.LastChild;
                            if (r != null)
                            {
                                XmlNode textLatLons = r.SelectSingleNode("Response/Route/Leg/Shape");
                                if (textLatLons != null)
                                {
                                    string[] latlons = textLatLons.InnerText.Split(new char[0]);
                                    foreach (string latlon in latlons)
                                    {
                                        char[] splchars = new char[] { (char)44 };
                                        string[] ll = latlon.Split(splchars);
                                        double lat = double.Parse(ll[0], CultureInfo.InvariantCulture);
                                        double lng = double.Parse(ll[1], CultureInfo.InvariantCulture);
                                        if (points == null)
                                        {
                                            points = new List<PointLatLng>();
                                        }
                                        points.Add(new PointLatLng(lat, lng));
                                    }

                                }

                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetRoutePoints: " + ex);
            }

            return points;
        }


        static readonly string RoutingUrlFormat = "https://route.api.here.com/routing/7.2/calculateroute.xml?app_id={6}&app_code={7}&waypoint0=geo!{0},{1}&waypoint1=geo!{2},{3}&mode={4};{5};traffic:disabled&legAttributes=shape";

        static readonly string FastestStr = "fastest";
        static readonly string ShortestStr = "shortest";

        static readonly string TruckStr = "truck";
        static readonly string CarStr = "car";
        #endregion

        #endregion

        #region GeocodingProvider Members

        public GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList)
        {
            return GetLatLngFromGeocoderUrl(MakeGeocoderUrl(keywords), out pointList);
        }

        public PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status)
        {
            List<PointLatLng> pointList;
            status = GetPoints(keywords, out pointList);
            return pointList != null && pointList.Count > 0 ? pointList[0] : (PointLatLng?)null;
        }

        public GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList)
        {
            return GetLatLngFromGeocoderUrl(MakeDetailedGeocoderUrl(placemark), out pointList);
        }

        public PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status)
        {
            List<PointLatLng> pointList;
            status = GetPoints(placemark, out pointList);
            return pointList != null && pointList.Count > 0 ? pointList[0] : (PointLatLng?)null;
        }

        public GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList)
        {
            throw new NotImplementedException("use GetPlacemark");
        }

        public Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status)
        {
            return GetPlacemarkFromReverseGeocoderUrl(MakeReverseGeocoderUrl(location), out status);
        }

        GeoCoderStatusCode GetLatLngFromGeocoderUrl(string url, out List<PointLatLng> pointList)

        {
            var status = GeoCoderStatusCode.Unknow;
            pointList = null;

            try
            {
                string geo = GMaps.Instance.UseGeocoderCache ? Cache.Instance.GetContent(url, CacheType.GeocoderCache) : string.Empty;

                bool cache = false;

                if (string.IsNullOrEmpty(geo))
                {
                    geo = GetContentUsingHttp(url);

                    if (!string.IsNullOrEmpty(geo))
                    {
                        cache = true;
                    }
                }

                if (!string.IsNullOrEmpty(geo))
                {
                    if (geo.StartsWith("<?xml") && geo.Contains("<ns2:Search") && geo.Contains("<Response") && geo.Contains("<Result"))
                    {
                        if (cache && GMaps.Instance.UseGeocoderCache)
                        {
                            Cache.Instance.SaveContent(url, CacheType.GeocoderCache, geo);
                        }

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(geo);
                        {
                            XmlNode r = doc.LastChild;
                            if (r != null)
                            {
                                XmlNodeList xnList = r.SelectNodes("Response/View/Result");
                                foreach (XmlNode xn in xnList)
                                {
                                    XmlNode ll = xn.SelectSingleNode("Location/DisplayPosition");
                                    if (ll != null)
                                    {
                                        XmlNode l = ll.SelectSingleNode("Latitude");
                                        if (l != null)
                                        {
                                            double lat = double.Parse(l.InnerText, CultureInfo.InvariantCulture);

                                            l = ll.SelectSingleNode("Longitude");
                                            if (l != null)
                                            {
                                                double lng = double.Parse(l.InnerText, CultureInfo.InvariantCulture);
                                                if (pointList == null)
                                                {
                                                    pointList = new List<PointLatLng>();
                                                }
                                                pointList.Add(new PointLatLng(lat, lng));
                                            }
                                        }
                                    }
                                }

                            }
                        }

                        status = GeoCoderStatusCode.G_GEO_SUCCESS;
                    }
                }

            }
            catch (Exception ex)
            {
                status = GeoCoderStatusCode.ExceptionInCode;
                //Debug.WriteLine("GetLatLngFromGeocoderUrl: " + ex);
            }

            return status;
        }

        Placemark? GetPlacemarkFromReverseGeocoderUrl(string url, out GeoCoderStatusCode status)
        {
            status = GeoCoderStatusCode.Unknow;
            Placemark? ret = null;

            try
            {
                string geo = GMaps.Instance.UsePlacemarkCache ? Cache.Instance.GetContent(url, CacheType.PlacemarkCache) : string.Empty;

                bool cache = false;

                if (string.IsNullOrEmpty(geo))
                {
                    geo = GetContentUsingHttp(url);

                    if (!string.IsNullOrEmpty(geo))
                    {
                        cache = true;
                    }
                }

                if (!string.IsNullOrEmpty(geo))
                {
                    if (geo.StartsWith("<?xml") && geo.Contains("<ns2:Search") && geo.Contains("<Response") && geo.Contains("<Result"))
                    {
                        if (cache && GMaps.Instance.UsePlacemarkCache)
                        {
                            Cache.Instance.SaveContent(url, CacheType.PlacemarkCache, geo);
                        }

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(geo);
                        {
                            //XmlNode r = doc.SelectSingleNode("Search/Response/View/Result/Location/Address");
                            XmlNode r = doc.LastChild;
                            if (r != null)

                            {
                                XmlNode a = r.SelectSingleNode("Response/View/Result/Location/Address/Label");//View/Result/Location/Address/Label\
                                if (a != null)
                                {
                                    var p = new Placemark(a.InnerText);

                                    XmlNode ad = r.SelectSingleNode("Response/View/Result/Location/Address");//View/Result/Location/Address/Label
                                    if (ad != null)
                                    {
                                        var vl = ad.SelectSingleNode("Country");
                                        if (vl != null)
                                        {
                                            p.CountryNameCode = vl.InnerText;
                                        }

                                        vl = ad.SelectSingleNode("PostalCode");
                                        if (vl != null)
                                        {
                                            p.PostalCodeNumber = vl.InnerText;
                                        }

                                        vl = ad.SelectSingleNode("County");
                                        if (vl != null)
                                        {
                                            p.AdministrativeAreaName = vl.InnerText;
                                        }

                                        vl = ad.SelectSingleNode("District");
                                        if (vl != null)
                                        {
                                            p.SubAdministrativeAreaName = vl.InnerText;
                                        }

                                        vl = ad.SelectSingleNode("City");
                                        if (vl != null)
                                        {
                                            p.LocalityName = vl.InnerText;
                                        }

                                        vl = ad.SelectSingleNode("Street");
                                        if (vl != null)
                                        {
                                            p.Street = vl.InnerText;
                                        }
                                        vl = ad.SelectSingleNode("HouseNumber");
                                        if (vl != null)
                                        {
                                            p.HouseNo = vl.InnerText;
                                        }

                                        vl = r.SelectSingleNode("Response/View/Result/Location/Address/AdditionalData[@key='CountryName']");
                                        if (vl != null)
                                        {
                                            p.CountryName = vl.InnerText;
                                        }
                                    }

                                    p.Address = string.Empty;
                                    if (!string.IsNullOrEmpty(p.Street))
                                    {
                                        p.Address += p.Street;
                                    }
                                    if (!string.IsNullOrEmpty(p.HouseNo))
                                    {
                                        p.Address += " " + p.HouseNo;
                                    }
                                    if (!string.IsNullOrEmpty(p.LocalityName))
                                    {
                                        if (!string.IsNullOrEmpty(p.Address))
                                        {
                                            p.Address += ", ";
                                        }
                                        p.Address += p.LocalityName;
                                    }
                                    if (!string.IsNullOrEmpty(p.AdministrativeAreaName))
                                        if (!string.IsNullOrEmpty(p.Address))
                                        {
                                            p.Address += ", ";
                                        }
                                    {
                                        p.Address += p.AdministrativeAreaName;
                                    }
                                    if (!string.IsNullOrEmpty(p.CountryName))
                                        if (!string.IsNullOrEmpty(p.Address))
                                        {
                                            p.Address += ", ";
                                        }
                                    {
                                        p.Address += p.CountryName;
                                    }

                                    ret = p;
                                    status = GeoCoderStatusCode.G_GEO_SUCCESS;
                                }


                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret = null;
                status = GeoCoderStatusCode.ExceptionInCode;
                //Debug.WriteLine("GetPlacemarkFromReverseGeocoderUrl: " + ex);
            }

            return ret;
        }

        #region -- internals --

        string MakeGeocoderUrl(string keywords)
        {
            return string.Format(GeocoderUrlFormat, keywords.Replace(' ', '+'), AppId, AppCode);
        }

        string MakeReverseGeocoderUrl(PointLatLng pt)
        {
            return string.Format(CultureInfo.InvariantCulture, ReverseGeocoderUrlFormat, pt.Lat, pt.Lng, AppId, AppCode);
        }

        string MakeDetailedGeocoderUrl(Placemark placemark)
        {
            var street = String.Join(" ", new[] { placemark.HouseNo, placemark.ThoroughfareName }).Trim();

            return string.Format(GeocoderDetailedUrlFormat,
                                 street.Replace(' ', '+'),
                                 placemark.LocalityName.Replace(' ', '+'),
                                 placemark.SubAdministrativeAreaName.Replace(' ', '+'),
                                 placemark.AdministrativeAreaName.Replace(' ', '+'),
                                 placemark.CountryName.Replace(' ', '+'),
                                 placemark.PostalCodeNumber.Replace(' ', '+'),
                                 AppId, AppCode);
        }

        static readonly string ReverseGeocoderUrlFormat = "https://reverse.geocoder.api.here.com/6.2/reversegeocode.xml?prox={0}%2C{1}%2C39&mode=retrieveAddresses&maxresults=1&&app_id={2}&app_code={3}";
        static readonly string GeocoderUrlFormat = "https://geocoder.api.here.com/6.2/geocode.xml?app_id={1}&app_code={2}&searchtext={0}";
        static readonly string GeocoderDetailedUrlFormat = "https://geocoder.api.here.com/6.2/geocode.xml?app_id={6}&app_code={7}&searchtext={0}+{1}+{2}+{3}+{4}+{5}";

        #endregion

        #endregion
    }


    /// <summary>
    /// HereMap provider
    /// </summary>
    public class HereMapProvider : HereMapProviderBase
    {
        public static readonly HereMapProvider Instance;

        HereMapProvider()
        {
        }

        static HereMapProvider()
        {
            Instance = new HereMapProvider();
        }

        #region GMapProvider Members

        readonly Guid id = new Guid("30DC2083-AC4D-4471-A232-D8A67AC9373A");
        public override Guid Id
        {
            get
            {
                return id;
            }
        }

        readonly string name = "HereMap";
        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = MakeTileImageUrl(pos, zoom, LanguageStr);

            return GetTileImageUsingHttp(url);
        }

        #endregion

        string MakeTileImageUrl(GPoint pos, int zoom, string language)
        {
            return string.Format(UrlFormat, UrlServerLetters[GetServerNum(pos, 4)], zoom, pos.X, pos.Y, AppId, AppCode);
        }

        //static readonly string UrlFormat = "https://{0}.traffic.maps.cit.api.here.com/maptile/2.1/traffictile/newest/normal.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
        static readonly string UrlFormat = "https://{0}.base.maps.api.here.com/maptile/2.1/maptile/newest/normal.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
    }
}