using System.Linq;
using AutoMapper;
using DatingApp.API.DTO;
using DatingApp.API.Models;

namespace DatingApp.API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserForListDto>()
                // check each member, handle PhotoUrl property of each member value is 'opt'(url)
                .ForMember(des => des.PhotoUrl, opt => {
                    // get a main photo from 'Photos' of 'User' class (src) and return to url
                    opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url);
                })
                .ForMember(des => des.Age, opt => {
                    // ResolveUsing: su dung value sau khi thuc hien callback
                    opt.ResolveUsing(d => d.DateOfBirth.CalculateAge());
                });
            CreateMap<User, UserForDetailedDto>()
            // check each member
                .ForMember(des => des.PhotoUrl, opt => {
                    // get a main photo from 'Photos' of 'User' class (src) and return to url
                    opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url);
                })
                .ForMember(des => des.Age, opt => {
                    opt.ResolveUsing(d => d.DateOfBirth.CalculateAge());
                });
            // CreateMap<[Source], [Destination]>
            CreateMap<Photo, PhotosForDetailedDto>();
            CreateMap<UserForUpdateDto, User>();
            CreateMap<Photo, PhotoForReturnDto>();
            CreateMap<PhotoForCreationDto, Photo>();
            CreateMap<UserForRegisterDto, User>();
            // ReverseMap: dao chieu
            CreateMap<MessageForCreationDto, Message>().ReverseMap();
            // check tung members, get photoUrl la main photo cua user (sender, recipient)
            CreateMap<Message, MessageToReturnDto>()
                .ForMember(m => m.SenderPhotoUrl, opt => opt
                    .MapFrom(u => u.Sender.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(m => m.RecipientPhotoUrl, opt => opt
                    .MapFrom(u => u.Recipient.Photos.FirstOrDefault(p => p.IsMain).Url));
        }
    }
}