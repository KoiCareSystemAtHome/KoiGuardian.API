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
        public async Task<List<TransactionPackageDto>> GetTransactionPackagebyOwnerId(Guid ownerid)
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

        [HttpGet("revenue-by-shop")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RevenueSummaryDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRevenueByShopId(Guid shopid, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await service.GetRevenueByShopIdAsync(shopid, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving revenue by shop: {ex.Message}");
            }
        }

        [HttpGet("total-revenue")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RevenueSummaryDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTotalRevenue(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await service.GetTotalRevenueAsync(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving total revenue: {ex.Message}");
            }
        }

        [HttpGet("order-status-summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderStatusSummaryDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrderStatusSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await service.GetOrderStatusSummaryAsync(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving order status summary: {ex.Message}");
            }
        }

        [HttpGet("order-status-summary-by-shop")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderStatusSummaryDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrderStatusSummaryByShopId(Guid shopid, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await service.GetOrderStatusSummaryByShopIdAsync(shopid, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving order status summary by shop: {ex.Message}");
            }
        }

        [HttpGet("product-sales-summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductSalesSummaryDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductSalesSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await service.GetProductSalesSummaryAsync(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving product sales summary: {ex.Message}");
            }
        }

        [HttpGet("product-sales-summary-by-shop")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductSalesSummaryDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductSalesSummaryByShopId(Guid shopid, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await service.GetProductSalesSummaryByShopIdAsync(shopid, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving product sales summary by shop: {ex.Message}");
            }
        }


    }
}
