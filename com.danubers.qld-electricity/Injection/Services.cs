using System;
using System.Collections.Generic;
using System.Globalization;
using Autofac;
using Danubers.QldElectricity.Services;
using Microsoft.AspNetCore.Routing.Internal;
using Newtonsoft.Json;

namespace Danubers.QldElectricity.Injection
{
    public class Services : Module
    {
        public Services()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultBackgroundService>().As<IBackgroundService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<DataService>().As<IDataService>().SingleInstance();
        }
    }

    internal class BomStationResponseModel
    {
        [JsonProperty("observations")]
        public BomStationsObservationsModel Observations { get; set; }
    }

    internal class BomStationsObservationsModel
    {
        [JsonProperty(PropertyName = "header")]
        public IEnumerable<BomStationResponseModelHeaderModel> Header { get; set; }
        [JsonProperty(PropertyName = "data")]
        public IEnumerable<BomStationReadingModel> Readings { get; set; }
    }

    internal class BomStationReadingModel
    {

        [JsonProperty("aifstime_utc")]
//        [JsonConverter(typeof(AIFSTImeConverter))]
        public string Timestamp { get; set; }
        [JsonProperty("sort_order")]
        public int SortOrder { get; set; }
        [JsonProperty("air_temp")]
        public float? AirTemp { get; set; }
        [JsonProperty("dewpt")]
        public float? Dewpoint { get; set; }
        [JsonProperty("cloud_oktas")]
        public int? CloudOktas { get; set; }
        [JsonProperty("wind_spd_kmh")]
        public int? WindSpeed { get; set; }
        [JsonProperty("wind_dir")]
        public string WindDir { get; set; }
    }

    internal class BomStationResponseModelHeaderModel
    {
        [JsonProperty(PropertyName = "ID")]
        public string ID { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string StationName { get; set; }
        [JsonProperty(PropertyName = "time_zone")]
        public string TimeZone { get; set; }

    }

    internal class EnergexPayload
    {
        public EnergexPayload() { }
        public EnergexPayload(string energex, int responseInt)
        {
            this.Type = energex;
            Timestamp = DateTime.UtcNow;
            Value = responseInt;
        }

        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
    }
}
