using Microsoft.AspNetCore.Mvc;
using ShopOnlineCore.Models;

namespace ShopOnlineCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// DELETE /api/admin/clear-users - Clear all users (Admin only, for dev use)
        /// </summary>
        [HttpDelete("clear-users")]
        public async Task<IActionResult> ClearUsers()
        {
            try
            {
                var users = _context.Users.ToList();
                _context.Users.RemoveRange(users);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Deleted {users.Count} users successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
