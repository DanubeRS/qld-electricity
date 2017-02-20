using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Danubers.QldElectricity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace Danubers.QldElectricity.Controllers
{
    /// <summary>
    /// Controller for colleciton of data
    /// </summary>
    [Route("api/data")]
    public class DataController : Controller
    {
        private readonly IDataService _dataService;

        public DataController(IDataService dataService)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Gets power consumption data, with an optional window specified
        /// </summary>
        /// <returns></returns>
        [HttpGet("power")]
        [ProducesResponseType(typeof(PowerDataResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponseModel),(int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetPowerData([FromQuery(Name = "begin")]DateTime? startDate = null, [FromQuery(Name = "end")]DateTime? endDate = null)
        {
            var dateTime = DateTime.UtcNow;
            if (startDate.HasValue && endDate.Value.ToUniversalTime() > dateTime.ToUniversalTime())
            {
                return BadRequest(new ErrorResponseModel
                {
                    Id = (int) ErrorCodes.BAD_DATE_RANGE,
                    Message = "Provided date range is not valid",
                    RequestId = Request.HttpContext.TraceIdentifier,
                    Payload = new
                    {
                        ServerDateTime = dateTime,
                        FailedCondition = "Date is in the future"
                    }
                });
            }
            if (startDate.HasValue && endDate.HasValue && startDate.Value.ToUniversalTime() >= endDate.Value.ToUniversalTime())
            {
                return BadRequest(new ErrorResponseModel
                {
                    Id = (int) ErrorCodes.BAD_DATE_RANGE,
                    Message = "Provided start date is later than the begin date",
                    RequestId = Request.HttpContext.TraceIdentifier,
                    Payload = new
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }
                });
            }
            var val = await _dataService.GetEnergyData(startDate.Value.ToUniversalTime(), endDate.Value.ToUniversalTime());
            var model = new PowerDataResponseModel()
            {
                StartTime = null,
                EndTime = null,
                Nodes = val.Select(v => new PowerDataNodeModel()
                {
                    Timestamp = v.Timestamp, 
                    Provider = "Energex",   //TODO
                    Value = new PowerDataNodeValueModel
                    {
                        Instantaneous = v.Value.Consumption
                    }
                }).ToArray()
            };
            return Ok(model);
        }

        /// <summary>
        /// Gets power consumption data, with an optional window specified
        /// </summary>
        /// <returns></returns>
        [HttpGet("weather/{stationId}")]
        [ProducesResponseType(typeof(WeatherDataResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponseModel), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetWeatherData(string stationId, [FromQuery(Name = "begin")]DateTime? startDate = null, [FromQuery(Name = "end")]DateTime? endDate = null)
        {
            var dateTime = DateTime.UtcNow;
            if (startDate.HasValue && endDate.Value.ToUniversalTime() > dateTime.ToUniversalTime())
            {
                return BadRequest(new ErrorResponseModel
                {
                    Id = (int)ErrorCodes.BAD_DATE_RANGE,
                    Message = "Provided date range is not valid",
                    RequestId = Request.HttpContext.TraceIdentifier,
                    Payload = new
                    {
                        ServerDateTime = dateTime,
                        FailedCondition = "Date is in the future"
                    }
                });
            }
            if (startDate.HasValue && endDate.HasValue && startDate.Value.ToUniversalTime() >= endDate.Value.ToUniversalTime())
            {
                return BadRequest(new ErrorResponseModel
                {
                    Id = (int)ErrorCodes.BAD_DATE_RANGE,
                    Message = "Provided start date is later than the begin date",
                    RequestId = Request.HttpContext.TraceIdentifier,
                    Payload = new
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }
                });
            }
            var val = await _dataService.GetWeatherData(stationId, startDate.Value.ToUniversalTime(), endDate.Value.ToUniversalTime());
            var model = new WeatherDataResponseModel()
            {
                StartTime = null,
                EndTime = null,
                Nodes = val.Select(v => new WeatherDataNodeModel()
                {
                    Timestamp = v.Timestamp,
                    Value = new WeatherDataNodeValueModel
                    {
                        AirTemp = v.Value.Temperature
                    }
                }).ToArray()
            };
            return Ok(model);
        }
    }

    public class WeatherDataResponseModel
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public WeatherDataNodeModel[] Nodes { get; set; }
    }

    public class WeatherDataNodeValueModel
    {
        public float AirTemp { get; set; }
    }

    public class WeatherDataNodeModel
    {
        public DateTime Timestamp { get; set; }
        public string Station { get; set; }
        public WeatherDataNodeValueModel Value { get; set; }
    }

    internal enum ErrorCodes
    {
        BAD_DATE_RANGE
    }

    /// <summary>
    /// Generic error response model, containing message, code, and other detailed information
    /// </summary>
    public class ErrorResponseModel
    {
        /// <summary>
        /// Error type Id
        /// </summary>
        public int Id { get; internal set; }
        /// <summary>
        /// Unique request Id
        /// </summary>
        public string RequestId { get; internal set; }
        /// <summary>
        /// Friendly error message
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// Additional error context payload
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Payload { get; internal set; }
    }

    /// <summary>
    /// Response model for power consumption data
    /// </summary>
    public class PowerDataResponseModel
    {
        /// <summary>
        /// Start time of the current model payload
        /// </summary>
        public DateTime? StartTime { get; internal set; }
        /// <summary>
        /// End time of the current model payload
        /// </summary>
        public DateTime? EndTime { get; internal set; }
        /// <summary>
        /// Data nodes constituting the current model
        /// </summary>
        public PowerDataNodeModel[] Nodes { get; internal set; }
    }

    /// <summary>
    /// Data node for power data, representing power statistics at a set timestamp
    /// </summary>
    public class PowerDataNodeModel
    {
        /// <summary>
        /// Timestamp of the current node model
        /// </summary>
        public DateTime Timestamp { get; internal set; }
        /// <summary>
        /// Provider of the power consumption data
        /// </summary>
        public string Provider { get; internal set; }
        /// <summary>
        /// Value of the power state at the timestamp
        /// </summary>
        public PowerDataNodeValueModel Value { get; internal set; }
    }

    /// <summary>
    /// Values (absolute and derived) of current power usage
    /// </summary>
    public class PowerDataNodeValueModel
    {
        /// <summary>
        /// Instantaneous absolute value of power consumption
        /// </summary>
        public int Instantaneous { get; internal set; }
        /// <summary>
        /// Cumulative power consumption, usually from a window defined in the parent payload
        /// </summary>
        public int Cumulative { get; internal set; }
        /// <summary>
        /// Change in power consumption since the last data node. Used to identify rate of power consumption change
        /// </summary>
        public int Trend { get; internal set; }
    }
}
