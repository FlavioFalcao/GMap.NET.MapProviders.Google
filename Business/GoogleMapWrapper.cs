using System.Collections.Generic;
using System.Linq;
using Google.Maps;
using Google.Maps.Geocoding;

namespace GMap.NET.MapProviders.Google.Business
{
    /// <summary>
    /// Contains static wrapper methods to convert between
    /// GMaps values and Google.Map values
    /// </summary>
    internal static class GoogleMapWrapper
    {

        /// <summary>
        /// Wrapps the given coordinates to GMap
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public static PointLatLng ToPointLatLng(LatLng coord)
        {
            return new PointLatLng(coord.Latitude, coord.Longitude);
        }


        /// <summary>
        /// Wraps the Google status codes to GMap status codes
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static GeoCoderStatusCode ToGeoCoderStatusCode(ServiceResponseStatus status)
        {
            switch (status)
            {
                case ServiceResponseStatus.Ok:
                    return GeoCoderStatusCode.G_GEO_SUCCESS;

                case ServiceResponseStatus.InvalidRequest:
                    return GeoCoderStatusCode.G_GEO_BAD_REQUEST;

                case ServiceResponseStatus.OverQueryLimit:
                    return GeoCoderStatusCode.G_GEO_TOO_MANY_QUERIES;

                case ServiceResponseStatus.RequestDenied:
                    return GeoCoderStatusCode.G_GEO_TOO_MANY_QUERIES;

                case ServiceResponseStatus.ZeroResults:
                    return GeoCoderStatusCode.G_GEO_UNKNOWN_ADDRESS;

                case ServiceResponseStatus.Unknown:
                    return GeoCoderStatusCode.Unknow;

                default:
                    return GeoCoderStatusCode.ExceptionInCode;
            }
        }

        /// <summary>
        /// Wraps the location type accuracy into a generic float value [0.0 - 1.0]
        /// </summary>
        /// <param name="locationType"></param>
        /// <returns></returns>
        public static float ToAccuracyValue(LocationType locationType)
        {
            switch (locationType)
            {
                case LocationType.Unknown:
                    return 0f;

                case LocationType.Approximate:
                    return 0.2f;

                case LocationType.GeometricCenter:
                    return 0.5f;

                case LocationType.RangeInterpolated:
                    return 0.8f;

                case LocationType.Rooftop:
                    return 1.0f;

                default:
                    return 0f;
            }
        }


        /// <summary>
        /// Wraps a Google Result into a Placemark
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Placemark ToPlacemark(Result result)
        {
            var placemark = new Placemark
                                {
                                    Address = result.FormattedAddress,
                                    HouseNo = FindAddressPart(result.AddressComponents, AddressType.StreetNumber),
                                    CountryName = FindAddressPart(result.AddressComponents, AddressType.Country),
                                    PostalCodeNumber = FindAddressPart(result.AddressComponents, AddressType.PostalCode),
                                    DistrictName = FindAddressPart(result.AddressComponents, AddressType.StreetAddress),
                                    ThoroughfareName = FindAddressPart(result.AddressComponents, AddressType.StreetAddress),
                                };

            return placemark;
        }


        /// <summary>
        /// Finds a part from address component
        /// </summary>
        /// <param name="components"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string FindAddressPart(IEnumerable<AddressComponent> components, AddressType type)
        {
            return (from component in components
                    where component.Types.Contains(type)
                    select component.LongName).FirstOrDefault();
        }

    }
}
