using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace WpfOly
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BooleanToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object thicknessIftrue, CultureInfo culture)
        {
            var state = (bool)value;
            int thickness = 0;
            Int32.TryParse(thicknessIftrue as String, out thickness);
            return state ? thickness : 0;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
