using System;

namespace DatingApp.API.DTO
{
    public class PhotosForDetailedDto
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsMain { get; set; } // check the main photo
        public bool IsApproved { get; set; } 
    }
}