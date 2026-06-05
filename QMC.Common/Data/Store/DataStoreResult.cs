using System;

namespace QMC.Common.Data.Store
{
    /// <summary>Data store 작업 결과입니다.</summary>
    public class DataStoreResult
    {
        public bool Success { get; protected set; }
        public string Path { get; protected set; }
        public string Message { get; protected set; }
        public Exception Exception { get; protected set; }

        public static DataStoreResult Ok(string path, string message = "")
        {
            try
            {
                return new DataStoreResult
                {
                    Success = true,
                    Path = path ?? string.Empty,
                    Message = message ?? string.Empty
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static DataStoreResult Fail(string path, string message, Exception exception = null)
        {
            try
            {
                return new DataStoreResult
                {
                    Success = false,
                    Path = path ?? string.Empty,
                    Message = message ?? string.Empty,
                    Exception = exception
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }

    /// <summary>Data store 로드 결과입니다.</summary>
    public class DataStoreResult<T> : DataStoreResult
    {
        public T Data { get; private set; }
        public bool UsedDefault { get; private set; }

        public static DataStoreResult<T> Ok(T data, string path, bool usedDefault, string message = "")
        {
            try
            {
                return new DataStoreResult<T>
                {
                    Success = true,
                    Path = path ?? string.Empty,
                    Message = message ?? string.Empty,
                    Data = data,
                    UsedDefault = usedDefault
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public new static DataStoreResult<T> Fail(string path, string message, Exception exception = null)
        {
            try
            {
                return new DataStoreResult<T>
                {
                    Success = false,
                    Path = path ?? string.Empty,
                    Message = message ?? string.Empty,
                    Exception = exception,
                    Data = default(T),
                    UsedDefault = false
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
