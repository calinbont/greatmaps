﻿
namespace GMap.NET.MapProviders
{
   using System;

   /// <summary>
   /// HereHybridMap provider
   /// </summary>
   public class HereHybridMapProvider : HereMapProviderBase
   {
      public static readonly HereHybridMapProvider Instance;

      HereHybridMapProvider()
      {
      }

      static HereHybridMapProvider()
      {
         Instance = new HereHybridMapProvider();
      }

      #region GMapProvider Members

      readonly Guid id = new Guid("B85A8FD7-40F4-40EE-9B45-491AA45D86C1");
      public override Guid Id
      {
         get
         {
            return id;
         }
      }

      readonly string name = "HereHybridMap";
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

        //static readonly string UrlFormat = "http://{0}.traffic.maps.cit.api.here.com/maptile/2.1/traffictile/newest/hybrid.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
        static readonly string UrlFormat = "https://{0}.aerial.maps.api.here.com/maptile/2.1/maptile/newest/hybrid.day/{1}/{2}/{3}/256/png8?lg=eng&app_id={4}&app_code={5}";
    }
}