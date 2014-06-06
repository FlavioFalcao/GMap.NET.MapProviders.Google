using System.Collections.Generic;
using System.Linq;
using Google.Maps;
using Google.Maps.Geocoding;

namespace GMap.NET.MapProviders.Google.Business
{
    internal static class GoogleMapWrapper
    {


        public static PointLatLng ToPointLatLng(LatLng coord)
        {
            return new PointLatLng(coord.Latitude, coord.Longitude);
        }



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


        private static string FindAddressPart(IEnumerable<AddressComponent> components, AddressType type)
        {
            return (from component in components
                    where component.Types.Contains(type)
                    select component.LongName).FirstOrDefault();
        }

    }
}
