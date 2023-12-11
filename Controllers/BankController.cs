using Azure.Core;
using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using static Azure.Core.HttpHeader;

namespace MetaverseMax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/trx/[controller]")]
    [Route("api/bnb/[controller]")]
    [Route("api/eth/[controller]")]
    public class BankController : ControllerBase
    {
        private readonly ILogger<AssetHistoryController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private readonly ServiceCommon common = new();

        public BankController(MetaverseMaxDbContext context, ILogger<AssetHistoryController> logger)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("GetBalance")]
        public IActionResult GetBalance([FromQuery] QueryParametersMatic parameters )
        {
            BankManage bankManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {

                return Ok(bankManage.GetBalance(parameters.matic_key));
            }

            return BadRequest("Get Bank Balance Call is invalid");       // 400 Error   
        }


        [HttpGet("ConfirmTransaction")]
        public IActionResult ConfirmTransaction([FromQuery] QueryParametersTransactionReceipt parameters)
        {
            BankManage bankManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {

                return Ok(bankManage.ConfirmTransaction(parameters.hash));
            }

            return BadRequest("Confirm Bank Transaction Call is invalid");       // 400 Error  
        }

        [HttpGet("WithdrawAllowanceApprove")]
        public IActionResult WithdrawAllowanceApprove([FromQuery] QueryParametersWithdrawAllowanceApprove parameters)
        {
            BankManage bankManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {

                return Ok(bankManage.WithdrawAllowanceApprove(parameters.amount, parameters.ownerMaticKey, parameters.personalSign));
            }

            return BadRequest("Confirm Bank Transaction Call is invalid");       // 400 Error  
        }

        [HttpGet("GetWithdrawSignCode")]
        public IActionResult GetWithdrawSignCode([FromQuery] QueryParametersWithdrawSignCode parameters)
        {
            BankManage bankManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(bankManage.GetWithdrawSignCode(parameters.amount, parameters.ownerMaticKey));  // "0x68656c6c6f" Hello in Hex UTF-8
            }

            return BadRequest("Confirm Bank Transaction Call is invalid");       // 400 Error  
        }
    }
}
