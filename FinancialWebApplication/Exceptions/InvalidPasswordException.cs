namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение валидности пароля.
    /// </summary>
    public class InvalidPasswordException: ApiException
    {
        /// <summary>
        /// Инициализация исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public InvalidPasswordException(string message = "Invalid password", int statusCode = 401)
        : base(statusCode, message)
        {
            
        }
    }
}
