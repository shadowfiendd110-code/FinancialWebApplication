using Microsoft.AspNetCore.Identity;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    /// <summary>
    /// Интерфейс для работы с паролями.
    /// </summary>
    public interface IPasswordHasherService
    {
        /// <summary>
        /// Хэширует пароль.
        /// </summary>
        /// <param name="password">Пароль.</param>
        /// <returns>Хэш пароля.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Проверяет валидность хэша пароля пользователя.
        /// </summary>
        /// <param name="hashedPassword">Хэш пароля.</param>
        /// <param name="password">Пароль.</param>
        /// <returns>True, если пароль валиден/False, если не валиден.</returns>
        bool VerifyHashedPassword(string hashedPassword, string password);
    }

    /// <summary>
    /// Сервис для работы с паролями.
    /// </summary>
    public class BCryptPasswordHasher : IPasswordHasherService
    {

        /// <summary>
        /// Инициализация сервиса.
        /// </summary>
        public BCryptPasswordHasher()
        {

        }

        /// <summary>
        /// Хэширует пароль.
        /// </summary>
        /// <param name="password">Пароль.</param>
        /// <returns>Хэш пароля.</returns>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Проверяет валидность хэша пароля пользователя.
        /// </summary>
        /// <param name="hashedPassword">Хэш пароля.</param>
        /// <param name="password">Пароль.</param>
        /// <returns>True, если пароль валиден/False, если не валиден.</returns>
        public bool VerifyHashedPassword(string hashedPassword, string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
