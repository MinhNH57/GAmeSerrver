using System;
using System.Collections.Generic;

namespace Server.Logging
{
    /// <summary>
    /// Interface cho Structured Logging — không phụ thuộc Serilog.
    /// Dùng chung cho mọi dự án, dễ mock trong unit test.
    /// </summary>
    public interface IStructuredLogger : IDisposable
    {
        /// <summary>
        /// Ghi log cấp Information
        /// </summary>
        /// <param name="messageTemplate">Template message (ví dụ: "RoleID {RoleID} mua vật phẩm {ItemName}")</param>
        /// <param name="propertyValues">Giá trị tương ứng cho các placeholder</param>
        void Information(string messageTemplate, params object[] propertyValues);

        /// <summary>
        /// Ghi log cấp Warning
        /// </summary>
        void Warning(string messageTemplate, params object[] propertyValues);

        /// <summary>
        /// Ghi log cấp Error
        /// </summary>
        void Error(string messageTemplate, params object[] propertyValues);

        /// <summary>
        /// Ghi log có kèm LogType (dùng cho game log category)
        /// </summary>
        /// <param name="logType">Category (ví dụ: "BaoThach", "LuckySpin"...)</param>
        /// <param name="messageTemplate">Template message</param>
        /// <param name="propertyValues">Giá trị tương ứng</param>
        void Write(string logType, string messageTemplate, params object[] propertyValues);

        /// <summary>
        /// Ghi log structured với cú pháp tuple gọn gàng.
        /// Mỗi tuple (key, value) sẽ trở thành field riêng biệt trong log.
        /// </summary>
        /// <param name="logType">Category (ví dụ: "Trade", "BaoThach"...)</param>
        /// <param name="message">Mô tả ngắn gọn sự kiện</param>
        /// <param name="fields">Các cặp (tên field, giá trị)</param>
        /// <example>
        /// logger.WriteStructured("Trade", "Giao dịch hoàn tất",
        ///     ("RoleID", roleId),
        ///     ("ItemID", itemId),
        ///     ("ItemName", "Kiếm Vàng"),
        ///     ("Quantity", 1),
        ///     ("GoldCost", 50000)
        /// );
        /// </example>
        void WriteStructured(string logType, string message, params (string key, object value)[] fields);

        /// <summary>
        /// Kiểm tra logger đã được khởi tạo thành công chưa
        /// </summary>
        bool IsEnabled { get; }
    }
}
