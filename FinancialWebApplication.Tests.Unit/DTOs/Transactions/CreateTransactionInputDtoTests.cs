using FinancialWebApplication.Tests.Unit.DTOs.Base;
using WebApplication3.DTOs.Transaction;
using WebApplication3.Models;

/// <summary>
/// Класс для тестирования DTO сущности Транзакция.
/// </summary>
public class CreateTransactionInputDtoBasicTests : DtoValidationTestBase<CreateTransactionInputDto>
{
    /// <summary>
    /// Создаёт валидный DTO транзакции для тестирования.
    /// </summary>
    /// <returns>Возвращает объект типа CreateTransactionInputDto.</returns>
    protected override CreateTransactionInputDto CreateValidDto()
    {
        return new CreateTransactionInputDto
        {
            Description = "Покупка продуктов",
            Sum = 1500,
            Type = TransactionType.Expense
        };
    }

    /// <summary>
    /// Проверяет, что "эталонный" объект действительно валиден.
    /// </summary>
    [Fact]
    public void ValidDto_ShouldBeValid() => AssertValid(CreateValidDto());

    /// <summary>
    /// Проверяет обязательнос описания у транзакции.
    /// </summary>
    [Fact]
    public void Description_Required_ShouldFailWhenEmpty()
    {
        var dto = CreateValidDto();
        dto.Description = "";
        AssertInvalid(dto, "Описание транзакции обязательно");
    }

    /// <summary>
    /// Проверяет диапазон суммы транзакции.
    /// </summary>
    [Fact]
    public void Sum_Range_ShouldFailWhenZero()
    {
        var dto = CreateValidDto();
        dto.Sum = 0;
        AssertInvalid(dto, "Сумма должна быть от 1 до 1 000 000");
    }

    /// <summary>
    /// Проверяет длину описания у транзакции.
    /// </summary>
    [Fact]
    public void Description_TooLong_ShouldFail()
    {
        var dto = CreateValidDto();
        dto.Description = new string('А', 101);
        AssertInvalid(dto, "Описание транзакции слишком длинное");
    }

    /// <summary>
    /// Проверяет сумму транзакции на положительность.
    /// </summary>
    [Fact]
    public void Sum_Range_ShouldFailWhenNegative()
    {
        var dto = CreateValidDto();
        dto.Sum = -1;
        AssertInvalid(dto, "Сумма должна быть от 1 до 1 000 000");
    }

    /// <summary>
    /// Проверяет значение типа транзакции по умолчанию у транзакции.
    /// </summary>
    [Fact]
    public void Type_DefaultValue_ShouldBeValid()
    {
        var dto = new CreateTransactionInputDto
        {
            Description = "Тест",
            Sum = 100
        };

        AssertValid(dto);
    }
}