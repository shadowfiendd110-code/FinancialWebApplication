namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение отсутствующей транзакции.
    /// </summary>
    public class TransactionNotFoundException: ApiException
    {
        /// <summary>
        /// Инициализация исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public TransactionNotFoundException(string message = "Транзакция не найдена", int statusCode = 404) : base(statusCode, message)
        {
            
        }
    }
}
