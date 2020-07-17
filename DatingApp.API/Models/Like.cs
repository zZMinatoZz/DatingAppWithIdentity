namespace DatingApp.API.Models
{
    public class Like
    {
        // id ng like
        public int LikerId { get; set; }
        // id ng dc like
        public int LikeeId { get; set; }
        public User Liker { get; set; }
        public User Likee { get; set; }
    }
}