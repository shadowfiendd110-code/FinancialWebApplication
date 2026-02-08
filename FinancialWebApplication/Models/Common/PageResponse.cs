namespace WebApplication3.Models.Common
{
    /// <summary>
    /// Страничный ответ API с поддержкой пагинации.
    /// </summary>
    /// <typeparam name="T">Тип ответа.</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>
        /// Текущий номер страницы.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Количество элементов на одной странице.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Общее количество страниц.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Общее количество записей в коллекции.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Признак наличия предыдущей страницы.
        /// </summary>
        public bool HasPrevious => PageNumber > 1;

        /// <summary>
        /// Признак наличия следующей страницы.
        /// </summary>
        public bool HasNext => PageNumber < TotalPages;

        /// <summary>
        /// Коллекция данных текущей страницы.
        /// </summary>
        public List<T> Data { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр страничного ответа.
        /// </summary>
        /// <param name="data">Данные текущей страницы.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Количество элементов на одной странице.</param>
        /// <param name="totalRecords">Общее количество записей в коллекции.</param>
        public PagedResponse(List<T> data, int pageNumber, int pageSize, int totalRecords)
        {
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRecords = totalRecords;
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        }
    }
}
