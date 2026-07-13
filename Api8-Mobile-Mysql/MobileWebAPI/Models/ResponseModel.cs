namespace MobileWebAPI.Models
{
    public class ResponseModel<T>
    {
        public T? Data { get; set; }
        public string Messages { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
