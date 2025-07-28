namespace Common.DTOs
{
    public class OperationResult<T>
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public T? Data { get; set; }

        public static OperationResult<T> Ok(T data) =>
            new OperationResult<T> { Success = true, Data = data };

        public static OperationResult<T> Fail(string error) =>
            new OperationResult<T> { Success = false, Error = error };
    }
}
