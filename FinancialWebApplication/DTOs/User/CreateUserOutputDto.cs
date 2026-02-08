namespace WebApplication3.DTOs.User
{
    /// <summary>
    /// Выходной DTO создания пользователя.
    /// </summary>
    public class CreatedUserOutputDto
    {
        /// <summary>
        /// Id пользователя.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя пользователя.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// E-mail пользователя.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public CreatedUserOutputDto() { }
    }
}
