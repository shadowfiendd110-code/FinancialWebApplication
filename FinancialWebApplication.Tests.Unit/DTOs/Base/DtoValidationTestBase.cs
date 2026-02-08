using System.ComponentModel.DataAnnotations;

namespace FinancialWebApplication.Tests.Unit.DTOs.Base
{
    /// <summary>
    /// Базовый класс для тестирования всех DTO.
    /// </summary>
    public abstract class DtoValidationTestBase<T> where T : class, new()
    {
        /// <summary>
        /// Создаёт валидный DTO для тестирования.
        /// </summary>
        protected abstract T CreateValidDto();

        /// <summary>
        /// Валидирует DTO и возвращает результат.
        /// </summary>
        /// <param name="dto">DTO для проверки.</param>
        /// <returns>Кастомный объект результата валидации.</returns>
        protected ValidationTestResult ValidateDto(T dto)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(dto);
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            return new ValidationTestResult 
            {
                IsValid = isValid,
                Errors = results
            };
        }

        /// <summary>
        /// Проверяет, что DTO валиден.
        /// </summary>
        /// <param name="dto">DTO для проверки.</param>
        protected void AssertValid(T dto)
        {
            var result = ValidateDto(dto);
            Assert.True(result.IsValid,
                $"DTO должен быть валидным. Ошибки: {GetErrorMessages(result.Errors)}");
        }

        /// <summary>
        /// Проверяет, что DTO невалиден и содержит ошибку.
        /// </summary>
        /// <param name="dto">DTO для проверки.</param>
        /// <param name="expectedErrorPart">Ожидаемая ошибка.</param>
        protected void AssertInvalid(T dto, string expectedErrorPart)
        {
            var result = ValidateDto(dto);
            Assert.False(result.IsValid, "DTO должен быть невалидным");
            Assert.Contains(result.Errors,
                e => e.ErrorMessage != null &&
                     e.ErrorMessage.Contains(expectedErrorPart, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Преобразует список ошибок в строку.
        /// </summary>
        /// <param name="errors">Ошибки.</param>
        /// <returns>Список ошибок в виде строки.</returns>
        private string GetErrorMessages(List<ValidationResult> errors)
        {
            return string.Join(", ", errors.Select(e => e.ErrorMessage));
        }
    }

    /// <summary>
    /// Результат валидации для тестов.
    /// </summary>
    public class ValidationTestResult
    {
        /// <summary>
        /// Валидный тест или нет.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Список ошибок.
        /// </summary>
        public List<ValidationResult> Errors { get; set; } = new();
    }
}