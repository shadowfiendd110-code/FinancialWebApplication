using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.Auth;

namespace FinancialWebApplication.Tests.Unit.DTOs.Auth
{
    /// <summary>
    /// Класс для тестирования login DTO сущности Пользователь.
    /// </summary>
    public class LoginUserDtoTests : DtoValidationTestBase<LoginUserDto>
    {
        /// <summary>
        /// Создаёт валидный login DTO пользователя для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа LoginUserDto.</returns>
        protected override LoginUserDto CreateValidDto()
        {
            return new LoginUserDto
            {
                Email = "user@example.com",
                Password = "password123"
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
        /// Проверяет Email на пустоту.
        /// </summary>
        [Fact]
        public void Email_Required_ShouldFailWhenEmpty()
        {
            var dto = CreateValidDto();
            dto.Email = "";
            AssertInvalid(dto, "Email обязателен");
        }

        /// <summary>
        /// Проверяет формат Email.
        /// </summary>
        [Fact]
        public void Email_InvalidFormat_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Email = "not-an-email";
            AssertInvalid(dto, "Неверный email");
        }

        /// <summary>
        /// Проверяет наличие пароля.
        /// </summary>
        [Fact]
        public void Password_Required_ShouldFailWhenEmpty()
        {
            var dto = CreateValidDto();
            dto.Password = "";
            AssertInvalid(dto, "Пароль обязателен");
        }
    }
}