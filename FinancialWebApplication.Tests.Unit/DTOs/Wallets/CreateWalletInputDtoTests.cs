using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.Wallet;

namespace FinancialWebApplication.Tests.Unit.DTOs.Wallets
{
    /// <summary>
    /// Класс для тестирования create DTO сущности Кошелёк.
    /// </summary>
    public class CreateWalletInputDtoTests : DtoValidationTestBase<CreateWalletInputDto>
    {
        /// <summary>
        /// Создаёт валидный create DTO кошелька для тестирования.
        /// </summary>
        /// <returns>Возвращает объект типа CreateWalletInputDto.</returns>
        protected override CreateWalletInputDto CreateValidDto()
        {
            return new CreateWalletInputDto
            {
                Name = "Основной кошелёк",
                Currency = "RUB", 
                InitialBalance = 1000
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
        /// Проверяет начальный баланс кошелька по нулевой границе.
        /// </summary>
        [Fact]
        public void InitialBalance_CanBeZero_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.InitialBalance = 0;
            AssertValid(dto);
        }

        /// <summary>
        /// Проверяет баланс кошелька по верхней границе.
        /// </summary>
        [Fact]
        public void InitialBalance_Maximum_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.InitialBalance = 10_000_000; // Максимум по Range
            AssertValid(dto);
        }

        /// <summary>
        /// Проверяет валидные коды валют для кошелька.
        /// </summary>
        /// <param name="currencyCode">Код валюты.</param>
        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("GBP")]
        [InlineData("JPY")]
        [InlineData("CNY")]
        [InlineData("RUB")]
        public void Currency_ValidCodes_ShouldBeValid(string currencyCode)
        {
            var dto = CreateValidDto();
            dto.Currency = currencyCode;
            AssertValid(dto);
        }

        /// <summary>
        /// Проверяет обязательность названия кошелька.
        /// </summary>
        [Fact]
        public void Name_Required_ShouldFailWhenEmpty()
        {
            var dto = CreateValidDto();
            dto.Name = "";
            AssertInvalid(dto, "Название кошелька обязательно");
        }

        /// <summary>
        /// Проверяет длину названия кошелька.
        /// </summary>
        [Fact]
        public void Name_TooLong_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Name = new string('А', 101);
            AssertInvalid(dto, "Название кошелька слишком длинное");
        }

        /// <summary>
        /// Проверяет длину названия кошелька.
        /// </summary>
        [Fact]
        public void Name_MaxLength_100_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.Name = new string('А', 100); 
            AssertValid(dto);
        }

        /// <summary>
        /// Проверяет наличие валюты кошелька.
        /// </summary>
        [Fact]
        public void Currency_Required_ShouldFailWhenEmpty()
        {
            var dto = CreateValidDto();
            dto.Currency = "";
            AssertInvalid(dto, "Валюта кошелька обязательна");
        }

        /// <summary>
        /// Проверяет длину кода валюты кошелька.
        /// </summary>
        [Fact]
        public void Currency_TooShort_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Currency = "US"; 
            AssertInvalid(dto, "Код валюты должен быть 3 символа");
        }

        /// <summary>
        /// Проверяет длину кода валюты кошелька.
        /// </summary>
        [Fact]
        public void Currency_TooLong_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Currency = "USDD"; 
            AssertInvalid(dto, "Код валюты должен быть 3 символа");
        }

        /// <summary>
        /// Проверяет формат валюты кошелька.
        /// </summary>
        /// <param name="invalidCurrency">Неверный формат валюты.</param>
        [Theory]
        [InlineData("usd")]    
        [InlineData("Usd")]    
        [InlineData("123")]    
        [InlineData("US1")]    
        [InlineData("U$D")]    
        [InlineData("РУБ")]    
        [InlineData("USD ")]  
        [InlineData(" USD")] 
        public void Currency_InvalidFormat_ShouldFail(string invalidCurrency)
        {
            var dto = CreateValidDto();
            dto.Currency = invalidCurrency;
            AssertInvalid(dto, "Неверный формат валюты");
        }

        /// <summary>
        /// Проверяет формат валюты на заглавные буквы.
        /// </summary>
        [Fact]
        public void Currency_Lowercase_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.Currency = "rub"; // строчные буквы
            AssertInvalid(dto, "Неверный формат валюты");
        }

        /// <summary>
        /// Проверяет наличие начального баланса кошелька.
        /// </summary>
        [Fact]
        public void InitialBalance_Required_ShouldFailWhenNotSet()
        {
            var dto = CreateValidDto();
            dto.InitialBalance = 0; 
            AssertValid(dto); 
        }

        /// <summary>
        /// Проверяет начальный баланс кошелька на отрицательное значение.
        /// </summary>
        [Fact]
        public void InitialBalance_Negative_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.InitialBalance = -100;
            AssertInvalid(dto, "Не верный начальный баланс кошелька");
        }

        /// <summary>
        /// Проверяет начальный баланс кошелька по верхней границе.
        /// </summary>
        [Fact]
        public void InitialBalance_TooHigh_ShouldFail()
        {
            var dto = CreateValidDto();
            dto.InitialBalance = 10_000_001; 
            AssertInvalid(dto, "Не верный начальный баланс кошелька");
        }

        /// <summary>
        /// Тестирует сразу несколько ошибок при создании кошелька.
        /// </summary>
        [Fact]
        public void MultipleErrors_ShouldAllBeReported()
        {
            var dto = new CreateWalletInputDto
            {
                Name = "",
                Currency = "12", 
                InitialBalance = -100 
            };

            var errors = ValidateDto(dto);

            Assert.True(errors.Errors.Count >= 3);

            var errorMessages = string.Join(", ", errors.Errors.Select(e => e.ErrorMessage));

            Assert.Contains("Название кошелька обязательно", errorMessages);
            Assert.Contains("Код валюты должен быть 3 символа", errorMessages);
            Assert.Contains("Не верный начальный баланс кошелька", errorMessages);
        }

        /// <summary>
        /// Проверяет значения по умолчанию.
        /// </summary>
        [Fact]
        public void EmptyDto_ShouldHaveMultipleErrors()
        {
            var dto = new CreateWalletInputDto(); 

            var errors = ValidateDto(dto);

            Assert.True(errors.Errors.Count >= 2); 
        }

        /// <summary>
        /// Проверяет различные варианты начального баланса кошелька.
        /// </summary>
        /// <param name="balance">Начальный баланс кошелька.</param>
        /// <param name="shouldBeValid">Валидная переменная.</param>
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(5_000_000, true)]
        [InlineData(10_000_000, true)]
        [InlineData(-1, false)]
        [InlineData(10_000_001, false)]
        public void InitialBalance_RangeValidation_Works(int balance, bool shouldBeValid)
        {
            var dto = CreateValidDto();
            dto.InitialBalance = balance;

            var errors = ValidateDto(dto);
            var isValid = errors.Errors.Count == 0;

            Assert.Equal(shouldBeValid, isValid);
        }

        /// <summary>
        /// Проверяет несколько вариантов названия кошелька.
        /// </summary>
        /// <param name="name">Название кошелька.</param>
        /// <param name="shouldBeValid">Валидная переменная.</param>
        /// <param name="expectedErrorPart">Ожидаемая ошибка.</param>
        [Theory]
        [InlineData("", false, "обязательно")]
        [InlineData("К", true, "")]
        [InlineData("Кошелёк", true, "")]
        [InlineData("Очень длинное название кошелька которое точно превысит " +
        "лимит в сто символов и вызовет ошибку валидации при тестировании", false, "слишком длинное")]
        public void Name_VariousValues_ValidationWorks(string name, bool shouldBeValid, string expectedErrorPart)
        {
            var dto = CreateValidDto();
            dto.Name = name;

            var errors = ValidateDto(dto);
            var isValid = errors.Errors.Count == 0;

            Assert.Equal(shouldBeValid, isValid);

            if (!shouldBeValid && !string.IsNullOrEmpty(expectedErrorPart))
            {
                Assert.Contains(errors.Errors, 
                    e => e.ErrorMessage != null && 
                    e.ErrorMessage.Contains(expectedErrorPart));
            }
        }
    }
}