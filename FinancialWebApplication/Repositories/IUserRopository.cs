using WebApplication3.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Exceptions;
using WebApplication3.DTOs.Filters;
using WebApplication3.Data;

namespace WebApplication3.Repositories
{
    /// <summary>
    /// Репозиторий для работы с пользователями.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Добавляет пользователя в БД.
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <returns>Пользователя.</returns>
        Task<User> AddUser(User user);

        /// <summary>
        /// Получает всех пользователей.
        /// </summary>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для пользователей.</param>
        /// <returns>Всех пользователей.</returns>
        Task<(List<User>, int)> GetAllUsers(UserFilter? filter = null, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Получает пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        /// <returns>Пользователя.</returns>
        Task<User> GetUser(int id);

        /// <summary>
        /// Удаляет пользователя из БД.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        Task DeleteUser(int id);

        /// <summary>
        /// Обновляет профиль пользователя.
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Обновлённого пользователя.</returns>
        Task<User> UpdateUserProfile(User user);

        /// <summary>
        /// Обновляет пароль пользователя.
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <param name="password">Пароль.</param>
        /// <returns>Обновленный пароль пользователя.</returns>
        Task ChangeUserPassword(User user, string password);

        /// <summary>
        /// Ищет пользователя по имени.
        /// </summary>
        /// <param name="email">Имя пользователя.</param>
        /// <returns>Пользователя.</returns>
        Task<User?> FindByEmail(string email);
    }

    /// <summary>
    /// Репозиторий для работы с пользователями.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        /// <summary>
        /// Контекст для работы с БД.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Создание репозитория.
        /// </summary>
        /// <param name="context">Контекст для работы с БД.</param>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ищет пользователя по email.
        /// </summary>
        /// <param name="email">Email пользователя.</param>
        /// <returns>Пользователя или null, если не найден.</returns>
        public async Task<User?> FindByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Обновляет пароль пользователя.
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <param name="passwordHash">Хэш пароля.</param>
        /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
        public async Task ChangeUserPassword(User user, string passwordHash)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);

            if (existingUser == null)
            {
                throw new UserNotFoundException();
            }

            existingUser.PasswordHash = passwordHash;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Обновляет пользователя.
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Обновлённого пользователя.</returns>
        public async Task<User> UpdateUserProfile(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);

            if (existingUser == null)
            {
                throw new UserNotFoundException();
            }

            existingUser.Name = user.Name;        
            existingUser.Email = user.Email;      

            await _context.SaveChangesAsync();

            return existingUser;
        }


        /// <summary>
        /// Добавляет пользователя в БД.
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <returns>Пользователя.</returns>
        public async Task<User> AddUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// Получает пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        /// <returns>Пользователя.</returns>
        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            return user;
        }

        /// <summary>
        /// Получает всех пользователей.
        /// </summary>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для пользователей.</param>
        /// <returns>Всех пользователей.</returns>
        public async Task<(List<User>, int)> GetAllUsers(UserFilter? filter = null, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<User> query = _context.Users;

            if(filter != null)
            {
                if(filter.HasEmailFilter)
                {
                    query = query.Where(u => u.Email == filter.EmailEquals!);
                }

                if(filter.HasNameFilter)
                {
                    query = query.Where(u => u.Name == filter.NameEquals!);
                }

                if(filter.HasRoleFilter)
                {
                    query = query.Where(u => u.Role == filter.RoleEquals!);
                }
            }

            var count = await query.CountAsync();

            var skip = (pageNumber - 1) * pageSize;

            var users = await query
                .OrderBy(x => x.Name)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (users, count);
        }

        /// <summary>
        /// Удаляет пользователя из БД.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        public async Task DeleteUser(int id)
        {
            var user = await GetUser(id);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
