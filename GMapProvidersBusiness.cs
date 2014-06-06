﻿using GMap.NET.MapProviders.Google.Business;

namespace GMap.NET.MapProviders.Google
{

    public static class GMapProvidersBusiness
    {
        public static readonly GoogleMapBusinessProvider GoogleMapBusiness = GoogleMapBusinessProvider.Instance;
        public static readonly GoogleMapBusinessProvider GoogleMapSateliteBusiness = GoogleSateliteMapBusinessProvider.Instance;
        public static readonly GoogleMapBusinessProvider GoogleMapHybridBusiness = GoogleHybridMapBusinessProvider.Instance;
        public static readonly GoogleMapBusinessProvider GoogleMapTerrainBusiness = GoogleTerrainMapBusinessProvider.Instance;

        public static GoogleMapBusinessProvider[] AllGoogleBusinessProviders = { GoogleMapBusiness, GoogleMapSateliteBusiness, GoogleMapHybridBusiness, GoogleMapTerrainBusiness };

        /// <summary>
        /// Set the credital string for all Google business providers
        /// </summary>
        public static string GoogleCreditalString
        {
            set
            {
                foreach (var provider in AllGoogleBusinessProviders)
                {
                    provider.CreditalString = value;
                }
            }
        }
    }
}
