using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace WebApplication3.Models
{
    /// <summary>
    /// Типы транзакций.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Доход.
        /// </summary>
        Income,

        /// <summary>
        /// Расход.
        /// </summary>
        Expense
    }
}



