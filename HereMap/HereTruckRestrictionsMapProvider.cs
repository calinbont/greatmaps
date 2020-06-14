
namespace GMap.NET.MapProviders
{
   using System;

   /// <summary>
   /// HereTruckRestrictionsMap provider
   /// </summary>
   public class HereTruckRestrictionsMapProvider : HereMapProviderBase
   {
      public static readonly HereTruckRestrictionsMapProvider Instance;

      HereTruckRestrictionsMapProvider()
      {
      }

      static HereTruckRestrictionsMapProvider()
      {
         Instance = new HereTruckRestrictionsMapProvider();
      }

      #region GMapProvider Members

      readonly Guid id = new Guid("AD9F2594-3E03-408E-9029-AC3094EA05E7");
      public override Guid Id
      {
         get
         {
            return id;
         }
      }

      readonly string name = "HereTruckRestrictionsMap";
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

         static readonly string UrlFormat = "https://{0}.base.maps.api.here.com/maptile/2.1/trucktile/newest/normal.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
    }
}