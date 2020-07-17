namespace DatingApp.API.Helpers
{
    public class MessageParams
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
        // status of message
        public string MessageContainer { get; set; } = "Unread";

    }
}