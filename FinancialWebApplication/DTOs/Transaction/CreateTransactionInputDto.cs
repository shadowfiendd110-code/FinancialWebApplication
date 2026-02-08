using System.ComponentModel.DataAnnotations;
using WebApplication3.Models;

namespace WebApplication3.DTOs.Transaction
{
    /// <summary>
    /// Входной DTO при создании транзакции.
    /// </summary>
    public class CreateTransactionInputDto
    {
        /// <summary>
        /// Описание транзакции.
        /// </summary>
        [Required(ErrorMessage = "Описание транзакции обязательно.")]
        [StringLength(100, ErrorMessage = "Описание транзакции слишком длинное")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Сумма транзакции.
        /// </summary>
        [Required(ErrorMessage = "Сумма транзакции обязательна.")]
        [Range(1, 1000000, ErrorMessage = "Сумма должна быть от 1 до 1 000 000")]
        public int Sum { get; set; }

        /// <summary>
        /// Тип транзакции.
        /// </summary>
        [Required(ErrorMessage = "Тип транзакции обязателен.")]
        public TransactionType Type { get; set; }

        /// <summary>
        /// Дата транзакции.
        /// </summary>
        public DateTime Date { get; set; } 

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public CreateTransactionInputDto() { }
    }
}
