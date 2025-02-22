using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;
    
public class AccountController(DataContext dataContext, ITokenService tokenService) : BaseApiController
{

[HttpPost("register")] // account/register
public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
{
   if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

    using var hmac = new HMACSHA512();

var user = new AppUser
{
    UserName = registerDto.Username.ToLower(),
    PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
    PasswordSalt = hmac.Key
};
dataContext.Users.Add(user);
await dataContext.SaveChangesAsync();
return new UserDto{
    Username = user.UserName,
    Token = tokenService.CreateToken(user)
};
}

[HttpPost("login")]
public async Task<ActionResult<UserDto>> Login(LoginDto loginDto, ITokenService tokenService) 
{
    var user = await dataContext.Users.FirstOrDefaultAsync( x => 
    x.UserName == loginDto.UserName.ToLower());

    if(user == null) return Unauthorized("Invalid username");

    using var hmac = new HMACSHA512(user.PasswordSalt);

    var computedhash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

    for (int i = 0; i < computedhash.Length; i++)
    {
        if(computedhash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
    }

    return new UserDto{
        Username = user.UserName,
        Token = tokenService.CreateToken(user)
    };
}


public async Task<bool> UserExists(string username){
    return await dataContext.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower());
}

}

