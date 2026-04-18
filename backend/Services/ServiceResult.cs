namespace MyFinBackend.Services
{
    public enum ServiceError { None, NotFound, Unauthorized, Conflict }

    public class ServiceResult
    {
        public ServiceError Error { get; }
        public bool IsSuccess => Error == ServiceError.None;

        protected ServiceResult(ServiceError error) => Error = error;

        public static ServiceResult Ok() => new(ServiceError.None);
        public static ServiceResult Fail(ServiceError error) => new(error);
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; }

        private ServiceResult(T? data, ServiceError error) : base(error) => Data = data;

        public static ServiceResult<T> Ok(T data) => new(data, ServiceError.None);
        public new static ServiceResult<T> Fail(ServiceError error) => new(default, error);
    }
}
