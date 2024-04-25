namespace Data_Layer.CustomModels
{
    public enum ResponseCode
    {
        Success = 200,
        Error = 500,
    }

    public class ServiceResponse
    {
        public ResponseCode StatusCode {  get; set; }
        public string? Message { get; set; }
    }
}