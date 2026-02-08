namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Базовое исключение для Api.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Код ошибки.
        /// </summary>
        public int StatusCode {  get; set; }

        /// <summary>
        /// Инициализация базового исключения.
        /// </summary>
        /// <param name="statusCode">Код ошибки.</param>
        /// <param name="message">Сообщение об ошибке.</param>
        public ApiException(int statusCode, string message) : base (message)
        {
            StatusCode = statusCode;
        }
    }
}
