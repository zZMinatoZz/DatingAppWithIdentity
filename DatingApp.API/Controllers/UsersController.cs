using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.DTO;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    // we added this at the top of the controller, any time that any of these methods get called
    // we are going to make activity log user action filter (update the last active properties)
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            // get userId from token
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            // get user form database with id
            var userFromRepo = await _repo.GetUser(currentUserId, true);

            userParams.UserId = currentUserId;

            if(string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }

            //[FromQuery] allow to send an empty query string
            var users = await _repo.GetUsers(userParams);
            // mapping from 'Users' to 'UserForListDto'
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);
            // response back to the client in response header
            Response.AddPagination(users.CurrentPage, users.PageSize, 
                users.TotalCount, users.TotalPages);

            // return users with status code OK
            return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name ="GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;

            var user = await _repo.GetUser(id, isCurrentUser);

            var userToReturn = _mapper.Map<UserForDetailedDto>(user);

            return Ok(userToReturn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            // khi client ban request len server se kem theo token,
            // thuc hien compare id get tu path url vs id trong token trong request
            if(id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }
            // get user theo id
            var userFromRepo = await _repo.GetUser(id, true);
            // update data form userForUpdateDto to userFromRepo
            _mapper.Map(userForUpdateDto, userFromRepo);

            // save changes
            if(await _repo.SaveAll()){
                return NoContent();
            }

            throw new Exception($"Updating user {id} failed on save");
        }
        // moi khi an like, ng nhan (recipient) se nhan like cua user
        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            // get current user id from token
            if(id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            
            var like = await _repo.GetLike(id, recipientId);
            // user (id) da like user (recipientId)
            if(like != null)
                return BadRequest("You already like this user");
            // tim kiem ng dc like
            if(await _repo.GetUser(recipientId, false) == null)
                return NotFound();
            // tao 1 object 'Like' de luu gia tri (user Id, user RecipientId)
            like = new Like{
                LikerId = id,
                LikeeId = recipientId
            };
            // luu gia tri
            _repo.Add<Like>(like);
            // luu vao database
            if (await _repo.SaveAll())
                return Ok();
            return BadRequest("Failed to like user");
        }
    }
}