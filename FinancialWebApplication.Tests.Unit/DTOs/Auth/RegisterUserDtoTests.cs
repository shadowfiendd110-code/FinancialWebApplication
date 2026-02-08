using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs;
using WebApplication3.DTOs.Auth;

namespace FinancialWebApplication.Tests.Unit.DTOs.Auth
{
    /// <summary>
    /// Класс для тестирования register DTO сущности Пользователь.
    /// </summary>
    public class RegisterUserDtoTests : DtoValidationTestBase<RegisterUserDto>
    {
        /// <summary>
        /// Создаёт валидный register DTO пользователя для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа RegisterUserDto.</returns>
        protected override RegisterUserDto CreateValidDto()
        {
            return new RegisterUserDto
            {
                Name = "Иван Иванов",
                Email = "ivan@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };
        }

        /// <summary>
        /// Проверяет, что "эталонный" объект действительно валиден.
        /// </summary>
        [Fact]
        public void ValidDto_ShouldBeValid()
        {
            var dto = CreateValidDto();
            AssertValid(dto);
        }

        /// <summary>
        /// Проверяет длину имени пользователя.
        /// </summary>
        [Fact]
        public void Name_Validation_ShouldWork()
        {
            var dto = CreateValidDto();
            dto.Name = "А";
            AssertInvalid(dto, "Имя должно быть от 2 до 100");

            dto = CreateValidDto();
            dto.Name = new string('А', 101);
            AssertInvalid(dto, "Имя должно быть от 2 до 100");
        }

        /// <summary>
        /// Проверяет корректность формата Email
        /// </summary>
        [Fact]
        public void Email_Validation_ShouldWork()
        {
            var dto = CreateValidDto();
            dto.Email = "invalid-email";
            AssertInvalid(dto, "Некорректный формат email");
        }

        /// <summary>
        /// Проверяет длину пароля.
        /// </summary>
        [Fact]
        public void Password_TooShort_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Password = "123";
            dto.ConfirmPassword = "123";
            AssertInvalid(dto, "Пароль должен быть не менее 8 символов");
        }

        /// <summary>
        /// Проверяет совпадение паролей.
        /// </summary>
        [Fact]
        public void ConfirmPassword_Mismatch_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.ConfirmPassword = "DifferentPassword!";
            AssertInvalid(dto, "Пароли не совпадают");
        }

        /// <summary>
        /// Проверяет на пустоту все обязательные поля.
        /// </summary>
        [Fact]
        public void AllFields_Required_ShouldFailWhenEmpty()
        {
            var dto = new RegisterUserDto(); // Все поля пустые
            var errors = ValidateDto(dto);

            // Должно быть минимум 4 ошибки (Name, Email, Password, ConfirmPassword)
            Assert.True(errors.Errors.Count >= 4);
        }
    }
}