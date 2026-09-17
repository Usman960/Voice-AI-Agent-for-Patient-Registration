namespace Voice_AI_Agent.DTOs
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public string? Error { get; set; }

        public static ApiResponse<T> Ok(T data) => new() { Data = data, Error = null };
        public static ApiResponse<T> Fail(string error) => new() { Data = default, Error = error };
    }
}
