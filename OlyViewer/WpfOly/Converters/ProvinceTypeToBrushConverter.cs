using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows;

namespace WpfOly
{
    class ProvinceTypeToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null) return null;

            var resourceKey = (string)values[0];
            var visited = false;
            visited = (bool)values[1];
            if (visited)
            {
                resourceKey += "_visited";
            }

            return Application.Current.Resources[resourceKey];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
