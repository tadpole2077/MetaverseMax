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
    [Route("api/trx/[controller]")]
    [Route("api/bnb/[controller]")]
    [Route("api/eth/[controller]")]
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
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                if (Task.Run(() => ownerManage.GetFromLandCoord(parameters.plotX, parameters.plotY)).Result != -1) 
                {
                    Task.Run(() => ownerManage.GetOwnerLands(ownerManage.ownerData.owner_matic_key)).Wait();
                }
                
                return Ok(ownerManage.ownerData);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetUsingMatic")]       
        public IActionResult GetUsingMatic([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                if (Task.Run(() => ownerManage.GetFromMaticKey(parameters.owner_matic_key)).Result != -1)
                {
                    Task.Run(() => ownerManage.GetOwnerLands(ownerManage.ownerData.owner_matic_key)).Wait();
                }

                return Ok(ownerManage.ownerData);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }


        [HttpGet("CheckHasPortfolio")]
        public IActionResult CheckHasPortfolio([FromQuery] QueryParametersOwnerDataKey parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {               
                return Ok( ownerManage.MatchOwner(parameters.owner_public_key) );
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }


        [HttpGet("GetOffer")]
        public IActionResult GetOffer([FromQuery] QueryParametersOwnerOffer parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => ownerManage.GetOwnerOffer(parameters.matic_key)).Result);                
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetCitizen")]
        public IActionResult GetCitizen([FromQuery] QueryParametersOwnerDataMaticRefresh parameters)
        {
            CitizenManage citizenManage = new(_context, common.IdentifyWorld(Request.Path));
            OwnerCitizenDB ownerCitizenDB = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                if (parameters.refresh)
                {
                    Task.Run(() => citizenManage.GetCitizenMCP(parameters.owner_matic_key)).Wait();
                    ownerCitizenDB.UpdateCitizenCount();
                    _context.SaveWithRetry();           // GetCitizenMCP call may not save datetime refresh changes due to optimisation of Datasync features.
                }

                return Ok(citizenManage.GetCitizen(parameters.owner_matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetCitizenMCP")]
        public IActionResult GetCitizenMCP([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            CitizenManage citizenManage = new(_context, common.IdentifyWorld(Request.Path));
            OwnerCitizenDB ownerCitizenDB = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                Task.Run(() => citizenManage.GetCitizenMCP(parameters.owner_matic_key)).Wait();

                return Ok(ownerCitizenDB.UpdateCitizenCount());
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetPet")]
        public IActionResult GetPet([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            CitizenManage citizenManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(citizenManage.GetPortfolioPets(parameters.owner_matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetPetMCP")]
        public IActionResult GetPetMCP([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            CitizenManage citizenManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => citizenManage.GetPetMCP(parameters.owner_matic_key)).Result);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetPetAllMCP")]
        public IActionResult GetPetAllMCP([FromQuery] QueryParametersSecurity parameters)
        {
            CitizenManage citizenManage = new(_context, common.IdentifyWorld(Request.Path));

            // As this service could be abused as a DDOS a security token is needed.                     
            if (ModelState.IsValid && parameters.secure_token.Equals("JUST_SIMPLE_CHECK123"))
            {
                return Ok(Task.Run(() => citizenManage.GetPetAllMCP()).Result);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

    }
}
