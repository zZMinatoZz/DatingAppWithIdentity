using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.DTO;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper,

        IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repo = repo;
            // create new account cloudinary with config values get from 'appsettings.json'
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);

        }
        // thuc hien get photo vua dc upload
        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            // get photo from databse with id
            var photoFromRepo = await _repo.GetPhoto(id);
            // map 2 kieu du lieu,  
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId,
            [FromForm] PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            // get user theo id
            var userFromRepo = await _repo.GetUser(userId, true);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        // chinh sua lai size image
                        Transformation = new Transformation()
                        .Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    // upload file to cloudinary
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            // get info from image uploaded to cloudinary and set to photoCreationDto
            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;
            // map 2 kieu du lieu <Photo> va <PhotoForCreationDto>
            var photo = _mapper.Map<Photo>(photoForCreationDto);

            // neu photos cua user k co cai nao la main, set photo uploaded len cloud la main
            if (!userFromRepo.Photos.Any(p => p.IsMain))
            {
                photo.IsMain = true;
            }
            // add vao list photos cua user
            userFromRepo.Photos.Add(photo);

            // save vao database
            if (await _repo.SaveAll())
            {
                // sau khi save vao data, thuc hien get data cua 'photo' vua dc save, store vao 'photoToReturn'
                // goi ham 'CreatedAtRoute', truyen vao route name (la ham GetPhoto() o tren) va 1 object (chua photo id va data get dc tu photo)
                // CreatedAtRoute() tra ve status code 201 (created) va url cua new resource duoc them moi (photo)
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                // tra ve status code 201 created va data cua tai nguyen created
                return CreatedAtRoute("GetPhoto", new { id = photo.Id }, photoToReturn);
            }
            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            var user = await _repo.GetUser(userId, true);
            // check neu k co photo nao cua user co id trung vs photo id trong url
            if (!user.Photos.Any(p => p.Id == id))
            {
                return Unauthorized();
            }

            var photoFromRepo = await _repo.GetPhoto(id);
            if (photoFromRepo.IsMain)
            {
                return BadRequest("This is already the main photo");
            }
            // get current main photo of user with userId
            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);
            // set old main photo is false
            currentMainPhoto.IsMain = false;
            // set new photo is the main photo of user
            photoFromRepo.IsMain = true;
            // save to database
            if (await _repo.SaveAll())
            {
                return NoContent();
            }
            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repo.GetUser(userId, true);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await _repo.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");
            // delete truong hop data tren cloud
            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                {
                    _repo.Delete(photoFromRepo);
                }
            }
            // delete trong truong hop data k phai tren cloud
            if(photoFromRepo.PublicId == null)
            {
                _repo.Delete(photoFromRepo);
            }


            if (await _repo.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}