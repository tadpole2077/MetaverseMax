using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace MetaverseMax.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Route("api/trx/[controller]")]
    [Route("api/bnb/[controller]")]
    [Route("api/eth/[controller]")]

    public class TransactionController : Controller
    {
        private readonly MetaverseMaxDbContext _context;
        private ServiceCommon common = new();

        public TransactionController(MetaverseMaxDbContext context)
        {
            _context = context;
        }

        [HttpGet("SetSystemSetting")]
        public IActionResult SetSystemSetting([FromQuery] QueryParametersSystemSetting parameters)
        {            
            if (ModelState.IsValid)
            {
                return Ok(
                    common.JsendAssignJSONData(
                        common.SetSystemSetting(parameters.secure_token, parameters.setting_name, parameters.value)
                    ));
            }

            return BadRequest("Sync Failed");       // 400 Error     
        }

        [HttpGet("log")]
        public IActionResult log([FromQuery] QueryParametersTransaction parameters)
        {
            TransactionManage transactionManage = new TransactionManage(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(transactionManage.InsertLog(
                    parameters.hash, 
                    parameters.from_wallet, 
                    parameters.to_wallet, 
                    parameters.unit_type, 
                    parameters.unit_amount, 
                    parameters.value, 
                    parameters.status, 
                    parameters.blockchain, 
                    parameters.transaction_type,
                    parameters.token_id)); 
            }

            return BadRequest("log Failed");       // 400 Error     
        }

        [HttpGet("getLog")]
        public IActionResult getLogbyMatic([FromQuery] QueryParametersOwnerDataMatic parameters)
        {
            TransactionManage transactionManage = new TransactionManage(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(transactionManage.GetLogByOwnerMatic(parameters.owner_matic_key));
            }

            return BadRequest("Get log Failed");       // 400 Error     
        }

        [HttpGet("getMCPEndpoint")]
        public IActionResult getMCPEndpoint([FromQuery] QueryParametersEndpoint parameters)
        {
            TransactionManage transactionManage = new TransactionManage(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(transactionManage.GetMCPEndpoint(parameters.contract_name));
            }

            return BadRequest("log Failed");       // 400 Error     
        }

    }
}
