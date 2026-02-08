namespace WebApplication3.Exceptions
{
    /// <summary>
    /// Исключение отсутствия утверждения пользователя.
    /// </summary>
    public class UserClaimNotFoundException: ApiException
    {
        /// <summary>
        /// Инициализация исключения.
        /// </summary>
        /// <param name="claimType">Утверждение пользователя.</param>
        /// <param name="statusCode">Код ошибки.</param>
        public UserClaimNotFoundException(string claimType, int statusCode = 401)
        : base(statusCode, $"Required claim '{claimType}' not found")
        {
            
        }
    }
}
