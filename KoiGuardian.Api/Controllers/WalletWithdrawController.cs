using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using KoiGuardian.Api.Services;
using KoiGuardian.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KoiGuardian.Models.Request;

namespace KoiGuardian.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletWithdrawController : ControllerBase
    {
        private readonly IWalletWithdrawService _walletWithdrawService;

        public WalletWithdrawController(IWalletWithdrawService walletWithdrawService)
        {
            _walletWithdrawService = walletWithdrawService;
        }

        /// <summary>
        /// Creates a new wallet withdrawal request.
        /// </summary>
        /// <param name="request">The withdrawal request details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A response indicating success or failure.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateWalletWithdraw([FromBody] CreateWalletWithdrawRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (message, isSuccess) = await _walletWithdrawService.CreateWalletWithdraw(request.UserId, request.Amount, cancellationToken);

            if (!isSuccess)
            {
                return BadRequest(new { Message = message });
            }

            return Ok(new { Message = message });
        }

        /// <summary>
        /// Updates the status of an existing wallet withdrawal request.
        /// </summary>
        /// <param name="withdrawId">The ID of the withdrawal request.</param>
        /// <param name="request">The update request containing the new status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A response indicating success or failure.</returns>
        [HttpPut("{withdrawId}")]
        public async Task<IActionResult> UpdateWalletWithdraw(Guid withdrawId, [FromBody] UpdateWalletWithdrawRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (message, isSuccess) = await _walletWithdrawService.UpdateWalletWithdraw(withdrawId, request.Status, cancellationToken);

            if (!isSuccess)
            {
                return BadRequest(new { Message = message });
            }

            return Ok(new { Message = message });
        }

        /// <summary>
        /// Retrieves all wallet withdrawal requests for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of wallet withdrawal responses.</returns>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetWalletWithdrawByUserId(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { Message = "User ID is required." });
            }

            var result = await _walletWithdrawService.GetWalletWithdrawByUserId(userId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all wallet withdrawal requests for a specific shop.
        /// </summary>
        /// <param name="shopId">The ID of the shop.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of wallet withdrawal responses including transactions.</returns>
        [HttpGet("shop/{shopId}")]
        public async Task<IActionResult> GetWalletWithdrawByShopId(Guid shopId, CancellationToken cancellationToken)
        {
            var result = await _walletWithdrawService.GetWalletWithdrawByShopId(shopId, cancellationToken);
            return Ok(result);
        }
    }

  
}