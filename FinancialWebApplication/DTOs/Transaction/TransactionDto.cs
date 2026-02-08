using System.ComponentModel.DataAnnotations;
using WebApplication3.Models;

namespace WebApplication3.DTOs.Transaction
{
    /// <summary>
    /// DTO транзакции.
    /// </summary>
    public class TransactionDto
    {
        /// <summary>
        /// Id транзакции.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Описание транзакции.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Сумма транзакции.
        /// </summary>
        public int Sum { get; set; }

        /// <summary>
        /// Дата транзакции.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Тип транзакции.
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Id кошелька.
        /// </summary>
        public int WalletId { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public TransactionDto() { }
    }
}   
