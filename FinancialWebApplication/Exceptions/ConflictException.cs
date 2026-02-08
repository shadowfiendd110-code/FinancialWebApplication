namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение конфликта бизнесс-логики.
    /// </summary>
    public class ConflictException: ApiException
    {
        /// <summary>
        /// Инициализация исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public ConflictException(string message, int statusCode = 409): base(statusCode, message)
        {
            
        }
    }
}
