using KoiGuardian.Api.Constants;
using KoiGuardian.Api.Services;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController
        (ITransactionService service) 
        : ControllerBase
    {
        [HttpGet("transaction-by-shop")]
        public async Task<List<TransactionDto>> GetTransactionbyShopId(Guid shopid)
        {
            return await service.GetTransactionbyShopIdAsync(shopid);
        }


        [HttpGet("package-transaction-by-owner")]
        public async Task<List<TransactionDto>> GetTransactionPackagebyOwnerId(Guid ownerid)
        {
            return await service.GetTransactionPackagebyOwnerIdAsync(ownerid);
        }


        [HttpGet("desposit-transaction-by-owner")]
        public async Task<List<TransactionDto>> GetTransactionDespositbyOwnerId(Guid ownerid)
        {
            return await service.GetTransactionDespositbyOwnerIdAsync(ownerid);
        }

        [HttpGet("order-transaction-by-owner")]
        public async Task<List<TransactionDto>> GetTransactionOrderbyOwnerId(Guid ownerid)
        {
            return await service.GetTransactionOrderbyOwnerIdAsync(ownerid);
        }

    }
}
