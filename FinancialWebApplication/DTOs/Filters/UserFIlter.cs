namespace WebApplication3.DTOs.Filters
{
    /// <summary>
    /// Фильтр по сущности Пользователь.
    /// </summary>
    public class UserFilter
    {
        /// <summary>
        /// Фильтрация по имени пользователя.
        /// </summary>
        public string? NameEquals { get; set; }

        /// <summary>
        /// Фильтрация по роли пользователя.
        /// </summary>
        public string? RoleEquals { get; set; }

        /// <summary>
        /// Фильтрация по E-mail пользователя.
        /// </summary>
        public string? EmailEquals { get; set; }

        /// <summary>
        /// Имя пользователя не должно быть пустым или равным null.
        /// </summary>
        public bool HasNameFilter => !string.IsNullOrWhiteSpace(NameEquals);

        /// <summary>
        /// E-mail пользователя не должен быть пустым или равным null.
        /// </summary>
        public bool HasEmailFilter => !string.IsNullOrWhiteSpace(EmailEquals);

        /// <summary>
        /// Роль пользователя не должна быть пустой или равной null.
        /// </summary>
        public bool HasRoleFilter => !string.IsNullOrWhiteSpace(RoleEquals);
    }
}
