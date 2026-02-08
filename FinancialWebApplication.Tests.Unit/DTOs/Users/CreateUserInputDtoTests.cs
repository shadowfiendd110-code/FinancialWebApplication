
using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.User;

namespace FinancialWebApplication.Tests.Unit.DTOs.Users
{
    /// <summary>
    /// Класс для тестирования create DTO сущности Пользователь.
    /// </summary>
    public class CreateUserInputDtoTests : DtoValidationTestBase<CreateUserInputDto>
    {
        /// <summary>
        /// Создаёт валидный create DTO пользователя для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа CreateUserInputDto.</returns>
        protected override CreateUserInputDto CreateValidDto()
        {
            return new CreateUserInputDto
            {
                Name = "Мария Петрова",
                Email = "maria@example.com",
                Password = "SecurePass123!",
                ConfirmPassword = "SecurePass123!"
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
        /// Проверяет длину Email.
        /// </summary>
        [Fact]
        public void Email_TooLong_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Email = new string('a', 250) + "@example.com"; // >256 символов
            AssertInvalid(dto, "Email слишком длинный");
        }

        /// <summary>
        /// Проверяет совпадают ли пароли.
        /// </summary>
        [Fact]
        public void ConfirmPassword_Compare_ShouldWork()
        {
            var dto = CreateValidDto();
            dto.ConfirmPassword = "WrongPassword";

            var validationResult = ValidateDto(dto);
            Assert.Contains(validationResult.Errors,
                e => e.ErrorMessage != null &&
                     e.ErrorMessage.Contains("Пароли не совпадают"));
        }
    }
}