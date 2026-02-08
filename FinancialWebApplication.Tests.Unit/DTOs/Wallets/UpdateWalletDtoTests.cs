using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.Wallet;

namespace FinancialWebApplication.Tests.Unit.DTOs.Wallets
{
    /// <summary>
    /// Класс для тестирования update DTO сущности Кошелёк.
    /// </summary>
    public class UpdateWalletDtoTests : DtoValidationTestBase<UpdateWalletDto>
    {
        /// <summary>
        /// Создаёт валидный update DTO кошелька для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа UpdateWalletDto.</returns>
        protected override UpdateWalletDto CreateValidDto()
        {
            return new UpdateWalletDto
            {
                Name = "Обновлённый кошелёк",
                Currency = "EUR"
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
        /// Проверяет наличие названия кошелька.
        /// </summary>
        [Fact]
        public void Name_Required_ShouldFailWhenEmpty()
        {
            var dto = CreateValidDto();
            dto.Name = "";
            AssertInvalid(dto, "Название кошелька обязательно");
        }

        /// <summary>
        /// Проверяет формат валюты кошелька.
        /// </summary>
        [Fact]
        public void Currency_ValidFormat_ShouldPass()
        {
            var validCurrencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "RUB" };

            foreach (var currency in validCurrencies)
            {
                var dto = CreateValidDto();
                dto.Currency = currency;
                AssertValid(dto);
            }
        }

        /// <summary>
        /// Проверяет валидность написания формата валюты кошелька.
        /// </summary>
        /// <param name="invalidCurrency">Не валидное написание формата валюты.</param>
        [Theory]
        [InlineData("")]      
        [InlineData("US")]    
        [InlineData("USDD")]  
        [InlineData("usd")]   
        [InlineData("123")]  
        [InlineData("U$D")] 
        public void Currency_InvalidValues_ShouldFail(string invalidCurrency)
        {
            var dto = CreateValidDto();
            dto.Currency = invalidCurrency;

            var errors = ValidateDto(dto);
            Assert.NotEmpty(errors.Errors);
        }

        /// <summary>
        /// Проверяет длину формата валюты.
        /// </summary>
        [Fact]
        public void Currency_WrongLength_ShowsCorrectErrorMessage()
        {
            var dto = CreateValidDto();
            dto.Currency = "US"; 

            var errors = ValidateDto(dto);
            Assert.Contains(errors.Errors, e => e.ErrorMessage != null && e.ErrorMessage.Contains("Код валюты должен быть 3 символа"));
        }
    }
}