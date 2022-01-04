using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScrcpyNet.Sample.Wpf
{
    public class VisibilityValueConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">True to invert the visibility.</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && targetType == typeof(Visibility))
            {
                if (parameter is bool invert && invert)
                    b = !b;
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v && targetType == typeof(bool))
            {
                bool b = v == Visibility.Visible;
                if (parameter is bool invert && invert)
                    b = !b;
                return b;
            }

            throw new NotSupportedException();
        }
    }
}
