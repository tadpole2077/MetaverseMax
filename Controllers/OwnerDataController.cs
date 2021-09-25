using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetaverseMax.ServiceClass;
using MetaverseMax.Database;

namespace MetaverseMax.Controllers
{
    //[Route("[controller]")]
    [ApiController]
    [Route("api/[controller]")]
    public class OwnerDataController : ControllerBase
    {
        private readonly ILogger<OwnerDataController> _logger;
        private readonly MetaverseMaxDbContext _context;

        private OwnerData ownerData = new();
        private Common common = new();

        public OwnerDataController(MetaverseMaxDbContext context, ILogger<OwnerDataController> logger)
        {
            _logger = logger;
            _context = context;

        }


        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersOwnerData parameters )
        {
            OwnerManage ownerManage = new(_context);
            if (ModelState.IsValid)
            {
                if (ownerManage.GetFromLandCoord(parameters.plotX, parameters.plotY) != -1) {
                    _ = ownerManage.GetOwnerLands(ownerManage.ownerData.owner_matic_key);
                }
                
                return Ok(ownerManage.ownerData);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetUsingMatic")]       
        public IActionResult GetUsingMatic([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            OwnerManage ownerManage = new(_context);

            if (ModelState.IsValid)
            {
                if (ownerManage.GetFromMaticKey(parameters.owner_matic_key) != -1)
                {
                    _ = ownerManage.GetOwnerLands(ownerManage.ownerData.owner_matic_key);
                }

                return Ok(ownerManage.ownerData);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }


        [HttpGet("CheckHasPortfolio")]
        public IActionResult CheckHasPortfolio([FromQuery] QueryParametersOwnerDataTron parameters)
        {
            OwnerManage ownerManage = new(_context);

            if (ModelState.IsValid)
            {               
                return Ok( ownerManage.CheckLocalDB_OwnerTron(parameters.owner_tron_public) );
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }


        [HttpGet("GetOffer")]
        public IActionResult GetOffer([FromQuery] QueryParametersOwnerOffer parameters)
        {
            OwnerManage ownerManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.GetOwnerOffer(parameters.active, parameters.matic_key));                
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetCitizen")]
        public IActionResult GetCitizen([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            CitizenManage citizenManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(citizenManage.GetCitizen(parameters.owner_matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetCitizenMCP")]
        public IActionResult GetCitizenMCP([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            CitizenManage citizenManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(citizenManage.GetCitizenMCP(parameters.owner_matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetPet")]
        public IActionResult GetPet([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            OwnerManage ownerManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.GetPet(parameters.owner_matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetPetMCP")]
        public IActionResult GetPetMCP([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            OwnerManage ownerManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.GetPetMCP(parameters.owner_matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

    }
}
