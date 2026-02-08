using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using WebApplication3.Repositories;
using WebApplication3.Models;
using WebApplication3.Exceptions;
using WebApplication3.DTOs.Filters;
using WebApplication3.Data;

namespace WebApplication3.Tests.Repositories
{
    /// <summary>
    /// Тесты для <see cref="UserRepository"/>.
    /// </summary>
    public class UserRepositoryTests : IDisposable
    {
        /// <summary>
        /// Контекст базы данных для тестирования.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Экземпляр тестируемого репозитория пользователей.
        /// </summary>
        private readonly UserRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserRepositoryTests"/>.
        /// </summary>
        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repository = new UserRepository(_context);
        }

        /// <summary>
        /// Освобождает ресурсы тестового класса.
        /// </summary>
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        /// <summary>
        /// Проверяет успешное нахождение пользователя по существующему email.
        /// </summary>
        [Fact]
        public async Task FindByEmail_ExistingEmail_ReturnsUser()
        {
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = "User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _repository.FindByEmail("test@example.com");

            result.Should().NotBeNull();
            result!.Email.Should().Be("test@example.com");
            result.Name.Should().Be("Test User");
        }

        /// <summary>
        /// Проверяет, что при поиске по несуществующему email возвращается null.
        /// </summary>
        [Fact]
        public async Task FindByEmail_NonExistingEmail_ReturnsNull()
        {
            var result = await _repository.FindByEmail("nonexistent@example.com");

            result.Should().BeNull();
        }

        /// <summary>
        /// Проверяет чувствительность к регистру при поиске пользователя по email.
        /// </summary>
        [Fact]
        public async Task FindByEmail_CaseSensitiveEmail_ReturnsCorrectUser()
        {
            var user = new User
            {
                Name = "Test",
                Email = "TEST@example.com",
                PasswordHash = "hash",
                Role = "User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _repository.FindByEmail("TEST@example.com");

            result.Should().NotBeNull();
        }

        /// <summary>
        /// Проверяет успешное добавление нового пользователя в базу данных.
        /// </summary>
        [Fact]
        public async Task AddUser_ValidUser_AddsToDatabase()
        {
            var user = new User
            {
                Name = "New User",
                Email = "new@example.com",
                PasswordHash = "hash",
                Role = "User"
            };

            var result = await _repository.AddUser(user);

            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Name.Should().Be("New User");
            result.Email.Should().Be("new@example.com");

            var dbUser = await _context.Users.FindAsync(result.Id);
            dbUser.Should().NotBeNull();
            dbUser!.Name.Should().Be("New User");
        }

        /// <summary>
        /// Проверяет успешное получение существующего пользователя по идентификатору.
        /// </summary>
        [Fact]
        public async Task GetUser_ExistingId_ReturnsUser()
        {
            var user = new User
            {
                Name = "Test",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = "User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _repository.GetUser(user.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(user.Id);
            result.Name.Should().Be("Test");
        }

        /// <summary>
        /// Проверяет, что при попытке получения несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetUser_NonExistingId_ThrowsUserNotFoundException()
        {
            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _repository.GetUser(999));
        }

        /// <summary>
        /// Проверяет успешное обновление профиля существующего пользователя.
        /// </summary>
        [Fact]
        public async Task UpdateUserProfile_ExistingUser_UpdatesProfile()
        {
            var user = new User
            {
                Name = "Old Name",
                Email = "old@example.com",
                PasswordHash = "hash",
                Role = "User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var updatedUser = new User
            {
                Id = user.Id,
                Name = "New Name",
                Email = "new@example.com",
                PasswordHash = user.PasswordHash,
                Role = user.Role
            };

            var result = await _repository.UpdateUserProfile(updatedUser);

            result.Should().NotBeNull();
            result.Name.Should().Be("New Name");
            result.Email.Should().Be("new@example.com");

            var dbUser = await _context.Users.FindAsync(user.Id);
            dbUser!.Name.Should().Be("New Name");
            dbUser.Email.Should().Be("new@example.com");
        }

        /// <summary>
        /// Проверяет, что при попытке обновления профиля несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task UpdateUserProfile_NonExistingUser_ThrowsUserNotFoundException()
        {
            var user = new User
            {
                Id = 999,
                Name = "Test",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = "User"
            };

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _repository.UpdateUserProfile(user));
        }

        /// <summary>
        /// Проверяет успешное изменение пароля существующего пользователя.
        /// </summary>
        [Fact]
        public async Task ChangeUserPassword_ExistingUser_UpdatesPassword()
        {
            var user = new User
            {
                Name = "Test",
                Email = "test@example.com",
                PasswordHash = "old_hash",
                Role = "User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var newPasswordHash = "new_hash";

            await _repository.ChangeUserPassword(user, newPasswordHash);

            var dbUser = await _context.Users.FindAsync(user.Id);
            dbUser!.PasswordHash.Should().Be(newPasswordHash);
        }

        /// <summary>
        /// Проверяет, что при попытке изменения пароля несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task ChangeUserPassword_NonExistingUser_ThrowsUserNotFoundException()
        {
            var user = new User
            {
                Id = 999,
                Name = "Test",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = "User"
            };

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _repository.ChangeUserPassword(user, "new_hash"));
        }

        /// <summary>
        /// Проверяет получение всех пользователей без фильтра.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_NoFilter_ReturnsAllUsers()
        {
            await SeedTestUsers(15);

            var (users, totalCount) = await _repository.GetAllUsers();

            totalCount.Should().Be(15);
            users.Should().HaveCount(10);
        }

        /// <summary>
        /// Проверяет получение пользователей с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithPagination_ReturnsCorrectPage()
        {
            await SeedTestUsers(25);

            var (users, totalCount) = await _repository.GetAllUsers(pageNumber: 2, pageSize: 5);

            totalCount.Should().Be(25);
            users.Should().HaveCount(5);
            users.All(u => u.Name.StartsWith("User")).Should().BeTrue();
        }

        /// <summary>
        /// Проверяет получение пользователей с фильтром по имени.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithNameFilter_ReturnsFilteredUsers()
        {
            await _context.Users.AddRangeAsync(
                new User { Name = "John", Email = "john@example.com", PasswordHash = "hash", Role = "User" },
                new User { Name = "Jane", Email = "jane@example.com", PasswordHash = "hash", Role = "User" },
                new User { Name = "Bob", Email = "bob@example.com", PasswordHash = "hash", Role = "User" }
            );
            await _context.SaveChangesAsync();

            var filter = new UserFilter { NameEquals = "John" };

            var (users, totalCount) = await _repository.GetAllUsers(filter);

            totalCount.Should().Be(1);
            users.Should().HaveCount(1);
            users.First().Name.Should().Be("John");
        }

        /// <summary>
        /// Проверяет получение пользователей с фильтром по email.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithEmailFilter_ReturnsFilteredUsers()
        {
            await _context.Users.AddRangeAsync(
                new User { Name = "User1", Email = "user1@example.com", PasswordHash = "hash", Role = "User" },
                new User { Name = "User2", Email = "user2@example.com", PasswordHash = "hash", Role = "User" }
            );
            await _context.SaveChangesAsync();

            var filter = new UserFilter { EmailEquals = "user1@example.com" };

            var (users, totalCount) = await _repository.GetAllUsers(filter);

            totalCount.Should().Be(1);
            users.Should().HaveCount(1);
            users.First().Email.Should().Be("user1@example.com");
        }

        /// <summary>
        /// Проверяет получение пользователей с фильтром по роли.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithRoleFilter_ReturnsFilteredUsers()
        {
            await _context.Users.AddRangeAsync(
                new User { Name = "Admin", Email = "admin@example.com", PasswordHash = "hash", Role = "Admin" },
                new User { Name = "User", Email = "user@example.com", PasswordHash = "hash", Role = "User" }
            );
            await _context.SaveChangesAsync();

            var filter = new UserFilter { RoleEquals = "Admin" };

            var (users, totalCount) = await _repository.GetAllUsers(filter);

            totalCount.Should().Be(1);
            users.Should().HaveCount(1);
            users.First().Role.Should().Be("Admin");
        }

        /// <summary>
        /// Проверяет получение пользователей с комбинированным фильтром.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithMultipleFilters_ReturnsFilteredUsers()
        {
            await _context.Users.AddRangeAsync(
                new User { Name = "John", Email = "john@example.com", PasswordHash = "hash", Role = "Admin" },
                new User { Name = "John", Email = "john2@example.com", PasswordHash = "hash", Role = "User" },
                new User { Name = "Jane", Email = "jane@example.com", PasswordHash = "hash", Role = "Admin" }
            );
            await _context.SaveChangesAsync();

            var filter = new UserFilter
            {
                NameEquals = "John",
                RoleEquals = "Admin"
            };

            var (users, totalCount) = await _repository.GetAllUsers(filter);

            totalCount.Should().Be(1);
            users.Should().HaveCount(1);
            users.First().Name.Should().Be("John");
            users.First().Role.Should().Be("Admin");
        }

        /// <summary>
        /// Проверяет, что пустые строки в фильтре игнорируются.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_EmptyStringFilter_IgnoresFilter()
        {
            await _context.Users.AddRangeAsync(
                new User { Name = "User1", Email = "user1@example.com", PasswordHash = "hash", Role = "User" },
                new User { Name = "User2", Email = "user2@example.com", PasswordHash = "hash", Role = "User" }
            );
            await _context.SaveChangesAsync();

            var filter = new UserFilter
            {
                NameEquals = "",
                EmailEquals = "  "
            };

            var (users, totalCount) = await _repository.GetAllUsers(filter);

            totalCount.Should().Be(2);
        }

        /// <summary>
        /// Проверяет успешное удаление существующего пользователя из базы данных.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ExistingUser_DeletesFromDatabase()
        {
            var user = new User
            {
                Name = "ToDelete",
                Email = "delete@example.com",
                PasswordHash = "hash",
                Role = "User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _repository.DeleteUser(user.Id);

            var dbUser = await _context.Users.FindAsync(user.Id);
            dbUser.Should().BeNull();
        }

        /// <summary>
        /// Проверяет, что при попытке удаления несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task DeleteUser_NonExistingUser_ThrowsUserNotFoundException()
        {
            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _repository.DeleteUser(999));
        }

        /// <summary>
        /// Создает тестовых пользователей в базе данных.
        /// </summary>
        /// <param name="count">Количество пользователей для создания.</param>
        private async Task SeedTestUsers(int count)
        {
            var users = Enumerable.Range(1, count)
                .Select(i => new User
                {
                    Name = $"User{i}",
                    Email = $"user{i}@example.com",
                    PasswordHash = $"hash{i}",
                    Role = i % 2 == 0 ? "Admin" : "User"
                })
                .ToList();

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
        }
    }
}