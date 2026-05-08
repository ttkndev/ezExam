using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ezExam.App.Converters
{
    /// <summary>
    /// Chuyển bool → text tùy chỉnh.
    /// ConverterParameter = "TrueText|FalseText"
    /// Ví dụ: "✅|" → true="✅", false=""
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = (parameter as string ?? "|").Split('|');
            var trueText = parts.Length > 0 ? parts[0] : "";
            var falseText = parts.Length > 1 ? parts[1] : "";
            return value is true ? trueText : falseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
