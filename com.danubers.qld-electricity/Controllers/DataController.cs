using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Danubers.QldElectricity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Danubers.QldElectricity.Controllers
{
    [Route("api/data")]
    public class DataController : Controller
    {
        private readonly IDataService _dataService;

        public DataController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [Route("power")]
        [HttpGet]
        public async Task<IActionResult> GetPowerData()
        {
            return Ok(await _dataService.GetEnergyData());
        }
    }
}
