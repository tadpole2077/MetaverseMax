﻿using Microsoft.AspNetCore.Mvc;
using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using MetaverseMax.BaseClass;

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
        private ServiceCommon common = new();

        public OwnerDataController(MetaverseMaxDbContext context, ILogger<OwnerDataController> logger)
        {
            _logger = logger;
            _context = context;
        }


        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersOwnerData parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                if (Task.Run(() => ownerManage.GetFromLandCoordMCP(parameters.plotX, parameters.plotY)).Result != -1)
                {
                    ownerManage.GetOwnerLands(ownerManage.ownerData.owner_matic_key, true, true);
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
                if (Task.Run(() => ownerManage.GetFromMaticKeyMCP(parameters.owner_matic_key)).Result != -1)
                {
                    ownerManage.GetOwnerLands(ownerManage.ownerData.owner_matic_key, true, true);
                }

                return Ok(ownerManage.ownerData);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        // Key Service on initial load of Page to identify Account holder
        [HttpGet("CheckHasPortfolio")]
        public IActionResult CheckHasPortfolio([FromQuery] QueryParametersOwnerDataKey parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(
                    common.JsendAssignJSONData( 
                        ownerManage.MatchOwner(parameters.owner_public_key) 
                        )
                    );
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("SetDarkMode")]
        public IActionResult SetDarkMode([FromQuery] QueryParametersOwnerDarkMode parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.UpdateDarkMode(parameters.owner_matic_key, parameters.dark_mode));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("SetBalanceVisible")]
        public IActionResult SetBalanceVisible([FromQuery] QueryParametersOwnerVisible parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.UpdateBalanceVisible(parameters.owner_matic_key, parameters.balance_visible));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }
        

        [HttpGet("GetOfferMCP")]
        public IActionResult GetOfferMCP([FromQuery] QueryParametersMatic parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => ownerManage.GetOwnerOfferMCP(parameters.matic_key)).Result);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetCitizen")]
        public IActionResult GetCitizen([FromQuery] QueryParametersOwnerDataMaticRefresh parameters)
        {
            CitizenManage citizenManage = new(_context, common.IdentifyWorld(Request.Path));
            OwnerCitizenDB ownerCitizenDB = new(_context, common.IdentifyWorld(Request.Path));
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                if (parameters.refresh && ownerManage.SetSlowDown(parameters.requester))
                {
                    Task.Run(() => citizenManage.GetOwnerCitizenCollectionMCP(parameters.owner_matic_key)).Wait();
                    ownerCitizenDB.UpdateCitizenCount();
                    citizenManage.CheckOwnerBuildingActive(parameters.owner_matic_key);
                    _context.SaveWithRetry();           // GetCitizenMCP call may not save datetime refresh changes due to optimisation of Datasync features.
                }

                return Ok(citizenManage.GetCitizen(parameters.owner_matic_key, parameters.requester));
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
                Task.Run(() => citizenManage.GetOwnerCitizenCollectionMCP(parameters.owner_matic_key)).Wait();

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


        [HttpGet("GetOwnerMaterialMatic")]
        public IActionResult GetOwnerMaterialMatic([FromQuery] QueryParametersSecurity parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            // As this service could be abused as a DDOS a security token is needed.                     
            if (ModelState.IsValid && parameters.secure_token.Equals("JUST_SIMPLE_CHECK123"))
            {
                return Ok(Task.Run(() => ownerManage.GetMaterialAllMatic()).Result);
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("UpdateOwnerAlert")]
        public IActionResult UpdateOwnerAlert([FromQuery] QueryParametersAlert parameters)
        {
            ServiceClass.AlertTriggerManager alertTrigger = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(alertTrigger.UpdateOwnerAlert(parameters.matic_key, parameters.alert_type, parameters.id, (ALERT_ACTION_TYPE)parameters.action));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetAlert")]
        public IActionResult GetAlert([FromQuery] QueryParametersAlertGet parameters)
        {
            ServiceClass.AlertTriggerManager alertTrigger = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(alertTrigger.GetByDistrict(parameters.matic_key, parameters.district_id));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetAlertSingle")]
        public IActionResult GetAlertSingle([FromQuery] QueryParametersAlertSingleGet parameters)
        {
            ServiceClass.AlertTriggerManager alertTrigger = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(alertTrigger.GetByType(parameters.matic_key, (ALERT_TYPE)parameters.alert_type, 0));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetPendingAlert")]
        public IActionResult GetPendingAlert([FromQuery] QueryParametersAlertPendingGet parameters)
        {
            AlertManage alert = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok( 
                    common.JsendAssignJSONData(
                        alert.Get(parameters.matic_key, (ALERT_STATE)parameters.pending_alert))
                        );
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("UpdateRead")]
        public IActionResult GetUpdateRead([FromQuery] QueryParametersMatic parameters)
        {
            AlertManage alert = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(alert.UpdateRead(parameters.matic_key));
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("DeletePendingAlert")]
        public IActionResult DeleteAlert([FromQuery] QueryParametersAlertDelete parameters)
        {
            AlertManage alert = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(common.JsendAssignJSONData( 
                    alert.Delete(parameters.matic_key, parameters.alert_pending_key))
                    );
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("GetOwnerWithName")]
        public IActionResult GetOwnerWithName()
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.GetOwnersWithName());
            }

            return BadRequest("Call is invalid");       // 400 Error   
        }

        [HttpGet("SyncWorldOwnerAll")]
        public IActionResult SyncWorldOwnerAll()
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                ownerManage.SyncWorldOwnerAll();

                return Ok(string.Concat("Sync completed"));
            }

            return BadRequest("Sync invalid");       // 400 Error     
        }


        [HttpGet("UnitTest_UpdateOwnerName")]
        public IActionResult UnitTest_UpdateOwnerName()
        {
            OwnerNameDB ownerNameDB = new OwnerNameDB(_context);

            if (ModelState.IsValid)
            {
                OwnerChange ownerChange = new() { owner_matic_key = "0xe4a746550e1ffb5f69775d3e413dbe1b5b734e36", owner_name = "TEST", owner_avatar_id = 0 };
                int rowCount = Task.Run(() => ownerNameDB.UpdateOwnerName(ownerChange, false)).Result;
                _context.SaveChanges();

                return Ok(string.Concat("Plots updated :", rowCount));
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }

        [HttpGet("UnitTest_AddAlertNewBuilding")]
        public IActionResult UnitTest_AddAlertNewBuilding()
        {
            AlertManage alert = new AlertManage(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                alert.UnitTest_AddAlertNewBuilding();
                return Ok(string.Concat("Unit test completed"));
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }


        [HttpGet("UnitTest_AddAlertRankingChange")]
        public IActionResult UnitTest_AddAlertRankingChange()
        {
            AlertManage alert = new AlertManage(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                alert.UnitTest_AddAlertRankingChange();
                return Ok(string.Concat("Unit test completed"));
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }

        [HttpGet("UnitTest_AddAlertTax")]
        public IActionResult UnitTest_AddAlertTax()
        {
            AlertManage alert = new AlertManage(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                alert.UnitTest_AddAlertTax();
                return Ok(string.Concat("Unit test completed"));
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }

        [HttpGet("UnitTest_AddOwnerUni")]
        public IActionResult UnitTest_AddOwnerUni()
        {
            MetaverseMaxDbContext_UNI _contextUni = new();            
            OwnerUniDB ownerUniDB = new(_contextUni);

            if (ModelState.IsValid)
            {
                ownerUniDB.NewOwner("test123", NETWORK.BINANCE_ID, common.IdentifyWorld(Request.Path));

                return Ok(string.Concat("Unit test completed"));
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }

    }
}
