namespace DatingApp.API.Helpers
{
    public class UserParams
    {
        // gioi han so luong 
        // trong truong hop client request qua nhieu data trong 1 page
        private const int MaxPageSize = 50;
        // set default value = 1
        public int PageNumber { get; set; } = 1;
        private int pageSize = 10;
        public int PageSize
        {
            get { return pageSize; }
            set { pageSize = (value > MaxPageSize) ? MaxPageSize : value; }
        }

        public int UserId { get; set; }
        public string Gender { get; set; }
        public int MinAge { get; set; } = 18;
        public int MaxAge { get; set; } = 99;
        
        public string OrderBy { get; set; }
        public bool Likees { get; set; } = false;

        public bool Likers { get; set; } = false;
        

    }
}