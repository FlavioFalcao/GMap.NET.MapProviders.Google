using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using GMap.NET.Projections;
using Google.Maps;
using Google.Maps.Direction;
using Google.Maps.Geocoding;
using Google.Maps.Internal;
using Google.Maps.StaticMaps;

namespace GMap.NET.MapProviders.Google.Business
{

    #region Other Google Map-Types wrapped as providers

    public class GoogleSateliteMapBusinessProvider : GoogleMapBusinessProvider
    {
        public new static readonly GoogleSateliteMapBusinessProvider Instance = new GoogleSateliteMapBusinessProvider();

        protected GoogleSateliteMapBusinessProvider()
            : base(MapTypes.Satellite)
        {
        }

        public override string Name
        {
            get { return "GoogleMap Satellite (Business)"; }
        }

        private readonly Guid _id = new Guid("0FD8CA5C-D00A-4140-A706-BE63B012CE7A");

        public override Guid Id
        {
            get { return _id; }
        }

    }

    public class GoogleHybridMapBusinessProvider : GoogleMapBusinessProvider
    {
        public new static readonly GoogleHybridMapBusinessProvider Instance = new GoogleHybridMapBusinessProvider();

        protected GoogleHybridMapBusinessProvider()
            : base(MapTypes.Hybrid)
        {
        }

        public override string Name
        {
            get { return "GoogleMap Hybrid (Business)"; }
        }

        private readonly Guid _id = new Guid("BCD7B456-5003-4183-A857-8CA29DD6D12A");

        public override Guid Id
        {
            get { return _id; }
        }
    }

    public class GoogleTerrainMapBusinessProvider : GoogleMapBusinessProvider
    {
        public new static readonly GoogleTerrainMapBusinessProvider Instance = new GoogleTerrainMapBusinessProvider();

        protected GoogleTerrainMapBusinessProvider()
            : base(MapTypes.Terrain)
        {
        }

        public override string Name
        {
            get { return "GoogleMap Terrain (Business)"; }
        }

        private readonly Guid _id = new Guid("2DD34DCC-9C1C-42E7-A1B9-D51891D939C7");

        public override Guid Id
        {
            get { return _id; }
        }
    }

    #endregion


    /// <summary>
    /// Google Maps for Business provider
    /// </summary>
    public class GoogleMapBusinessProvider : GMapProvider, GeocodingProvider, RoutingProvider
    {
        public static readonly GoogleMapBusinessProvider Instance = new GoogleMapBusinessProvider();

        private readonly MapTypes _mapType;

        #region Constructor

        protected GoogleMapBusinessProvider()
            : this(MapTypes.Roadmap)
        {
        }

        protected GoogleMapBusinessProvider(MapTypes mapType)
        {
            _mapType = mapType;
            Copyright = "Map Data ©" + DateTime.Now.Year + " Google";
            Http.Factory = new GMapsCachedHttpGetResponseFactory();
        }

        #endregion

        #region Google id and key

        private string _clientId;
        private string _privateKey;

        /// <summary>
        /// Set the Google ClientId and PrivateKey in order to use Google API for Business
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="privateKey"></param>
        internal void SetCreditals(string clientId, string privateKey)
        {
            _clientId = clientId;
            _privateKey = privateKey;

            if (!String.IsNullOrEmpty(_clientId) && !String.IsNullOrEmpty(_privateKey))
                GoogleSigned.AssignAllServices(new GoogleSigned(clientId, privateKey));
        }

        /// <summary>
        /// Gets or sets the credital string 
        /// Format: id=your-google-id;key=your-google-key
        /// </summary>
        internal string CreditalString
        {
            get { return "id=" + _clientId + ";key=" + _privateKey; }
            set
            {
                if (value == null) return;

                string id, key;
                if (ParseCredital(value, out id, out key))
                {
                    SetCreditals(id, key);
                }
                else
                {
                    throw new FormatException("CreditalString has an invalid format and could not be parsed: " + value);
                }
            }
        }

        private static bool ParseCredital(string credital, out string clientId, out string key)
        {
            bool success = false;
            clientId = null;
            key = null;

            var regex = new Regex("id=(.+);key=(.+)");
            var match = regex.Match(credital);
            if (match.Success)
            {
                clientId = match.Groups[1].Value;
                key = match.Groups[2].Value;
                success = true;
            }

            return success;
        }

        #endregion

        #region GMapProvider Members

        private readonly Guid _id = new Guid("1F5DB68E-1F81-4CF7-A78F-549480DBC90C");

        public override Guid Id
        {
            get { return _id; }
        }

        public override string Name
        {
            get { return "GoogleMap (Business)"; }
        }

        public override PureProjection Projection
        {
            get { return MercatorProjection.Instance; }
        }

        private GMapProvider[] _overlays;

        public override GMapProvider[] Overlays
        {
            get { return _overlays ?? (_overlays = new GMapProvider[] {this}); }
        }

        #endregion

        #region Tile Access Google Static Maps

        private static readonly Size DefaultTileSize = new Size(256, 256);
        private bool _removeBranding = true;

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            //return _googleMapProvider.GetTileImage(pos, zoom);

            return _removeBranding
                       ? GetTileImageWithoutBranding(pos, zoom)
                       : GetTileImage(pos, zoom, DefaultTileSize);
        }

        private PureImage GetTileImageWithoutBranding(GPoint pos, int zoom)
        {
            PureImage cleanTile = null;
            const int brandingHeight = 25;

            // we fetch a tile which is 25px bigger at the top as well as at the bottom

            var brandedTile = GetTileImage(pos, zoom, new Size(
                                                          DefaultTileSize.Width,
                                                          DefaultTileSize.Height + brandingHeight*2));

            // now we cut away the additional 25px at the upper and lower border
            // this will automatically also cut away the branding / copyright of the tile

            if (brandedTile != null)
            {
                using (brandedTile)
                using (var tempBitmap = new Bitmap(brandedTile.Data))
                using (var cropped = CropImage(tempBitmap, new Rectangle(
                                                               0, // x
                                                               brandingHeight, // y
                                                               DefaultTileSize.Width, // width
                                                               DefaultTileSize.Height))) // height
                {
                    var memoryStream = new MemoryStream();
                    cropped.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Position = 0;
                    cleanTile = TileImageProxy.FromStream(memoryStream);
                    cleanTile.Data = memoryStream;
                }
            }
            return cleanTile;
        }

        private static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            // An empty bitmap which will hold the cropped image
            var bmp = new Bitmap(section.Width, section.Height);

            using (var g = Graphics.FromImage(bmp))
            {
                // Draw the given area (section) of the source image
                // at location 0,0 on the empty bitmap (bmp)
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            }

            return bmp;
        }




        private PureImage GetTileImage(GPoint pos, int zoom, Size tileSize)
        {
            var tileCenterLatLng = TileToWorldPos(pos.X + 0.5, pos.Y + 0.5, zoom);

            var map = new StaticMapRequest
                          {
                              Center = new Location(tileCenterLatLng.Lat + "," + tileCenterLatLng.Lng),
                              MapType = _mapType,
                              Size = tileSize,
                              Zoom = zoom,
                              Sensor = false
                          };

            var imageUri = map.ToUri().ToString();

            if (GoogleSigned.SigningInstance != null)
                imageUri = GoogleSigned.SigningInstance.GetSignedUri(imageUri);


            PureImage image = null;
            try
            {
                image = GetTileImageUsingHttp(imageUri);
            }
            catch (WebException e)
            {
                // WebException such as 404 / Forbidden etc.
                Debug.WriteLine(e.Message);
            }

            return image;
        }

        /// <summary>
        /// Returns the Lat/Lng world coordinate from the given tile coordinate
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private PointLatLng TileToWorldPos(double tileX, double tileY, int zoom)
        {
            double n = Math.PI - ((2.0*Math.PI*tileY)/Math.Pow(2.0, zoom));

            var p = new PointLatLng
                        {
                            Lng = (tileX/Math.Pow(2.0, zoom)*360.0) - 180.0,
                            Lat = 180.0/Math.PI*Math.Atan(Math.Sinh(n))
                        };
            return p;
        }

        #endregion

        #region GeocodingProvider

        private readonly GeocodingService _geocodingService = new GeocodingService();


        public PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status)
        {
            float dummy;
            return GetPoint(keywords, out status, out dummy);
        }

        public PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status, out float accuracy)
        {
            PointLatLng? coordinate = null;
            accuracy = 0f;

            var request = new GeocodingRequest
            {
                Address = keywords,
                Sensor = false
            };

            var response = _geocodingService.GetResponse(request);

            status = GoogleMapWrapper.ToGeoCoderStatusCode(response.Status);

            if (response.Results.Any())
            {
                var geometry = response.Results[0].Geometry;
                coordinate = GoogleMapWrapper.ToPointLatLng(geometry.Location);
                accuracy = GoogleMapWrapper.ToAccuracyValue(geometry.LocationType);
            }

            return coordinate;
        }

        public GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList)
        {
            var request = new GeocodingRequest
                              {
                                  Address = keywords,
                                  Sensor = false
                              };

            var response = _geocodingService.GetResponse(request);

            pointList = (from r in response.Results
                         select GoogleMapWrapper.ToPointLatLng(r.Geometry.Location)).ToList();

            
            return GoogleMapWrapper.ToGeoCoderStatusCode(response.Status);
        }



        // reverse geo coding

        public GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList)
        {
            var request = new GeocodingRequest
                              {
                                  Address = new LatLng(location.Lat, location.Lng),
                                  Sensor = false
                              };

            var response = _geocodingService.GetResponse(request);


            placemarkList = (from r in response.Results
                             select GoogleMapWrapper.ToPlacemark(r)).ToList();

            return GoogleMapWrapper.ToGeoCoderStatusCode(response.Status);

        }

        public Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status)
        {
            var request = new GeocodingRequest
                              {
                                  Address = new LatLng(location.Lat, location.Lng),
                                  Sensor = false
                              };

            var response = _geocodingService.GetResponse(request);
            status = GoogleMapWrapper.ToGeoCoderStatusCode(response.Status);

            return response.Results.Any()
                       ? GoogleMapWrapper.ToPlacemark(response.Results[0])
                       : (Placemark?) null;
        }


        [Obsolete("use GetPoint(string keywords...", true)]
        public PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status)
        {
            throw new NotImplementedException("use GetPoint(string keywords...");
        }

        [Obsolete("use GetPoint(string keywords...", true)]
        public GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList)
        {
            throw new NotImplementedException("use GetPoint(string keywords...");
        }

        #endregion

        #region RoutingProvider

        public MapRoute GetRoute(PointLatLng start, PointLatLng end, bool avoidHighways, bool walkingMode, int zoom)
        {
            MapRoute route = null;
            var from = new LatLng(start.Lat, start.Lng);
            var to = new LatLng(end.Lat, end.Lng);

            var direcrtionsRequest = new DirectionRequest()
                                         {
                                             Origin = from,
                                             Destination = to,
                                             Sensor = false,
                                             Mode = walkingMode ? TravelMode.walking : TravelMode.driving
                                         };

            var directionService = new DirectionService();
            var directionsResponse = directionService.GetResponse(direcrtionsRequest);

            if (directionsResponse.Status == ServiceResponseStatus.Ok)
            {
                if (directionsResponse.Routes.Any())
                {
                    var directionsRoute = directionsResponse.Routes[0];
                    // TODO
                    var pathPoints = AssembleHQPolyline(directionsRoute);

                    var pnts = (from p in pathPoints
                                select new PointLatLng(p.Latitude, p.Longitude)).ToList();

                    route = new MapRoute(pnts,
                                         directionsRoute.Summary);
                }
            }

            return route;
        }

        public MapRoute GetRoute(string start, string end, bool avoidHighways, bool walkingMode, int zoom)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<LatLng> AssembleOverviewPolyline(DirectionRoute route)
        {
            var line = route.OverviewPolyline;
            return PolylineEncoder.Decode(line.Points);
        }

      

        private static IEnumerable<LatLng> AssembleHQPolyline(DirectionRoute route)
        {
            var hdRoute = new List<LatLng>();

            foreach (var leg in route.Legs)
            {
                foreach (var step in leg.Steps)
                {
                    hdRoute.AddRange(PolylineEncoder.Decode(step.Polyline.Points));
                }
            }

            return hdRoute;
        }

        #endregion
    }
}
