using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.DTO;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace DatingApp.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        // public IAuthRepository _repo { get; }
        public IConfiguration _config { get; }
        public AuthController(IConfiguration config, IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _mapper = mapper;
            // _repo = repo;
            _config = config;
            _signInManager = signInManager;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

            // if (await _repo.UserExists(userForRegisterDto.Username))
            //     return BadRequest("Username already exists");

            var usertoCreate = _mapper.Map<User>(userForRegisterDto);
            // create user identity with user (contains user data, username) and password
            var result = await _userManager.CreateAsync(usertoCreate, userForRegisterDto.Password);

            // var createdUser = await _repo.Register(usertoCreate, userForRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(usertoCreate);

            if (result.Succeeded)
            {
                // server response ve kem theo value
                return CreatedAtRoute("GetUser", new
                {
                    Controller = "Users",
                    id = usertoCreate.Id
                }, userToReturn);
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            // var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            // if (userFromRepo == null)
            //     return Unauthorized();

            var user = await _userManager.FindByNameAsync(userForLoginDto.Username);
            //lockoutOnFailure:
            //     Flag indicating if the user account should be locked if the sign in fails.
            var result = await _signInManager
                .CheckPasswordSignInAsync(user, userForLoginDto.Password, false);

            if (result.Succeeded)
            {
                // include list photos theo user logged response ve
                var appUser = await _userManager.Users.Include(p => p.Photos)
                    .FirstOrDefaultAsync(u => u.NormalizedUserName == userForLoginDto.Username.ToUpper());

                var userToReturn = _mapper.Map<UserForListDto>(appUser);

                //     return Ok(new
                //     {
                //         token = GenerateJwtToken(appUser);
                // });

                return Ok(new
                {
                    token = GenerateJwtToken(appUser).Result,
                    user = userToReturn
                });

            }

            return Unauthorized("Wrong Username or Password!!!");
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            // create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            // get all roles of user
            var roles = await _userManager.GetRolesAsync(user);


            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // create key, stored in appsetting.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.
                GetBytes(_config.GetSection("AppSettings:Token").Value));

            // create creds from 'key' encoded
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // create tokenDescriptor from claims, creds
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            // create token from tokenDescriptor
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}










