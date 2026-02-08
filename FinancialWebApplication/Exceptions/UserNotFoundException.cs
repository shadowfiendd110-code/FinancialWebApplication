namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение отсутствующего пользователя.
    /// </summary>
    public class UserNotFoundException : ApiException
    {
        /// <summary>
        /// Инициализация исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public UserNotFoundException(string message = "Пользователь не найден", int statusCode = 404): base(statusCode, message) 
        { 

        }
    }
}
