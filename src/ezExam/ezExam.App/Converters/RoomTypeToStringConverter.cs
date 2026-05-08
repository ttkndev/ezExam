using ezExam.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ezExam.App.Converters
{
    /// <summary>Chuyển RoomType enum → chuỗi tiếng Việt để hiển thị</summary>
    public class RoomTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is RoomType type ? type switch
            {
                RoomType.Literature => "Ngữ văn",
                RoomType.Math => "Toán",
                RoomType.Shift1 => "Ca 1",
                RoomType.Shift2 => "Ca 2",
                _ => value.ToString() ?? ""
            } : "";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
