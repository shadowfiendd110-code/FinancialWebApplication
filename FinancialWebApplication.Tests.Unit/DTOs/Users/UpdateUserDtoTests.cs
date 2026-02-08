using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.User;

namespace FinancialWebApplication.Tests.Unit.DTOs.Users
{
    /// <summary>
    /// Класс для тестирования update DTO сущности Пользователь.
    /// </summary>
    public class UpdateUserDtoTests : DtoValidationTestBase<UpdateUserDto>
    {
        /// <summary>
        /// Создаёт валидный update DTO пользователя для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа UpdateUserDto.</returns>
        protected override UpdateUserDto CreateValidDto()
        {
            return new UpdateUserDto
            {
                Id = 1,
                Name = "Обновлённое Имя",
                Email = "updated@example.com"
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
        /// Проверяет наличие Id обновляемого пользователя.
        /// </summary>
        [Fact]
        public void Id_Required_ShouldFailWhenZero()
        {
            var dto = CreateValidDto();
            dto.Id = 0; 

            var errors = ValidateDto(dto);
            Assert.Contains(errors.Errors, 
                e => e.ErrorMessage != null && 
                e.ErrorMessage.Contains("Id обязателен"));
        }

        /// <summary>
        /// Проверяет корректность формата Email.
        /// </summary>
        [Fact]
        public void Email_ValidFormat_ShouldPass()
        {
            var validEmails = new[]
            {
                "test@example.com",
                "user.name@domain.co.uk",
                "иван@яндекс.рф"
            };

            foreach (var email in validEmails)
            {
                var dto = CreateValidDto();
                dto.Email = email;
                AssertValid(dto);
            }
        }

        /// <summary>
        /// Проверяет корректность формата Email.
        /// </summary>
        /// <param name="invalidEmail">Не корректный формат Email.</param>
        [Theory]
        [InlineData("invalid")]
        [InlineData("test@")]
        [InlineData("test@domain")]
        [InlineData("test@.com")]
        public void Email_InvalidFormat_ShouldFail(string invalidEmail)
        {
            var dto = CreateValidDto();
            dto.Email = invalidEmail;
            AssertInvalid(dto, "Некорректный формат email");
        }

        /// <summary>
        /// Проверяет наличие Email.
        /// </summary>
        [Fact]
        public void Email_Empty_ShouldShowRequiredError()
        {
            var dto = CreateValidDto();
            dto.Email = "";
            AssertInvalid(dto, "Email обязателен");
        }

        /// <summary>
        /// Проверяет Email на null.
        /// </summary>
        [Fact]
        public void Email_Null_ShouldShowRequiredError()
        {
            var dto = CreateValidDto();
            dto.Email = null;
            AssertInvalid(dto, "Email обязателен");
        }
    }
}