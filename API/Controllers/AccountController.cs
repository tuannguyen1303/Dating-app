using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        public AccountController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> Register([FromBody] RegisterDto registerDto)
        {
            if (await UserExist(registerDto.Username)) return BadRequest("Username is taken");
            using var hmac = new HMACSHA512();

            var newUser = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AppUser>> Login([FromBody] LoginDto loginDto)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(m => m.UserName == loginDto.Username.ToLower());
            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (var i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }

            return user;
        }

        private async Task<bool> UserExist(string username)
        {
            return await _context.AppUsers.AnyAsync(o => o.UserName == username.ToLower());
        }
    }
}