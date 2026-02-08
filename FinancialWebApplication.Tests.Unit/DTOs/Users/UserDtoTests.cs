using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.User;

namespace FinancialWebApplication.Tests.Unit.DTOs.Users
{
    /// <summary>
    /// Класс для тестирования DTO сущности Пользователь.
    /// </summary>
    public class UserDtoTests : DtoValidationTestBase<UserDto>
    {
        /// <summary>
        /// Создаёт валидный DTO пользователя для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа UserDto.</returns>
        protected override UserDto CreateValidDto()
        {
            return new UserDto
            {
                Name = "Иван Иванов",
                Email = "ivan@example.com",
                Password = "Password123!"
            };
        }

        /// <summary>
        /// Проверяет, что "эталонный" объект действительно валиден.
        /// </summary>
        [Fact]
        public void ValidUserDto_ShouldBeValid()
        {
            var dto = CreateValidDto();
            AssertValid(dto);
        }

        /// <summary>
        /// Проверяет обязательность имени, а также его длину.
        /// </summary>
        [Fact]
        public void Name_RequiredAndLength_ValidationWorks()
        {
            var dto = CreateValidDto();
            dto.Name = "";
            AssertInvalid(dto, "Имя обязательно");

            dto = CreateValidDto();
            dto.Name = "А";
            AssertInvalid(dto, "Имя должно быть от 2 до 100");

            dto = CreateValidDto();
            dto.Name = new string('А', 101);
            AssertInvalid(dto, "Имя должно быть от 2 до 100");
        }

        /// <summary>
        /// Проверяет обязательность email, а также его формат.
        /// </summary>
        [Fact]
        public void Email_RequiredAndFormat_ValidationWorks()
        {
            var dto = CreateValidDto();
            dto.Email = "";
            AssertInvalid(dto, "Email обязателен");

            dto = CreateValidDto();
            dto.Email = "not-email";
            AssertInvalid(dto, "Некорректный формат email");
        }

        /// <summary>
        /// Проверяет обязательность пароля,а также его длину.
        /// </summary>
        [Fact]
        public void Password_RequiredAndLength_ValidationWorks()
        {
            var dto = CreateValidDto();
            dto.Password = "";
            AssertInvalid(dto, "Пароль обязателен");

            dto = CreateValidDto();
            dto.Password = "1234567";
            AssertInvalid(dto, "Пароль должен быть не менее 8");
        }
    }
}