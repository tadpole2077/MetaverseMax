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
        private readonly ILogger<PlotController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private ServiceCommon common = new();

        public TransactionController(MetaverseMaxDbContext context, ILogger<PlotController> logger)
        {
            _logger = logger;
            _context = context;
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
    }
}
