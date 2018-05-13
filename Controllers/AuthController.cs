using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        public AuthController(IAuthRepository repo, IConfiguration config, IMapper mapper)
        {
            _mapper = mapper;
            _config = config;
            _repo = repo;
        }
        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserForRegisterDto userForRegisterDto)
        {
            //if username return from the DTO is not empty, we turn that username to lowercase
            if (!string.IsNullOrEmpty(userForRegisterDto.Username))
                userForRegisterDto.Username = userForRegisterDto.Username.ToLower();
            //if user exist return error
            if (await _repo.UserExists(userForRegisterDto.Username))
                ModelState.AddModelError("Username", "The user is already taken");
            //validated request
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            //turn data from DTO to User model in the database using mapper
            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var createUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(createUser);
            //create a new route along with create a new user. the new route is /api/users/{id}. the GetUser will be specified in the UserController
            return CreatedAtRoute("GetUser", new {controller = "Users", id = createUser.Id}, userToReturn);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized();

            var tokenHandler = new JwtSecurityTokenHandler();
            //This  will get the key that we store in appsettings.json
            var key = Encoding.ASCII.GetBytes(_config.GetSection("AppSettings:Token").Value);
            // create token description include user id, username name, date expire
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name, userFromRepo.Username)
                }),
                //date expire is set to 1 day
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
            };
            //new token is created
            var token = tokenHandler.CreateToken(tokenDescriptor);
            //return the token as json string
            var tokenString = tokenHandler.WriteToken(token);

            var user = _mapper.Map<UserForListDto>(userFromRepo);
            //Ok function indicate we successful login, and the user and token have been pass to clietn site for us to use
            return Ok(new { tokenString, user });
        }
    }
}