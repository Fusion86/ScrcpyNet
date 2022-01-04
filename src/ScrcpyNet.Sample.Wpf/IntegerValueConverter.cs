﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace ScrcpyNet.Sample.Wpf
{
    public class IntegerValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString()!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && targetType == typeof(double) && double.TryParse(str, out var i))
            {
                return i;
            }

            throw new NotSupportedException();
        }
    }
}
