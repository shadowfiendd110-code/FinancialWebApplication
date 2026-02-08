namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение отсутствующего кошелька.
    /// </summary>
    public class WalletNotFoundException : ApiException
    {
        /// <summary>
        /// Инициализация исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public WalletNotFoundException(string message = "Кошелёк не найден", int statusCode = 404) : base(statusCode, message)
        {
            
        }
    }
}
