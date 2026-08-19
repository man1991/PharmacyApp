namespace PharmacyApp.Api.Exceptions
{
    /// <summary>
    /// Base type for all "expected" application errors (as opposed to unexpected bugs/crashes).
    /// Carries an HTTP status code so the global exception middleware knows how to respond,
    /// and a message that is safe to show directly to an end user.
    /// </summary>
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string userFriendlyMessage, int statusCode) : base(userFriendlyMessage)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>Thrown when a requested resource (e.g. a medicine) cannot be found.</summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound)
        {
        }
    }

    /// <summary>
    /// Thrown for business-rule violations that are the caller's fault
    /// (e.g. trying to sell more units than are in stock).
    /// </summary>
    public class BusinessRuleException : AppException
    {
        public BusinessRuleException(string message) : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }

    /// <summary>
    /// Thrown when the underlying JSON data store cannot be read or written.
    /// Kept distinct from validation errors so it maps to a 500-level response.
    /// </summary>
    public class DataStoreException : AppException
    {
        public DataStoreException(string message, Exception innerException)
            : base(message, StatusCodes.Status500InternalServerError)
        {
            HResult = innerException.HResult;
        }
    }
}
