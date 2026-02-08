namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение не валидного рефреш токена.
    /// </summary>
    public class InvalidRefreshTokenException : ApiException
    {
        /// <summary>
        /// Инициализация рефреш токена.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public InvalidRefreshTokenException(string message = "Invalid or expired refresh token", int statusCode = 401)
            : base(statusCode, message)
        {
        }
    }

}
