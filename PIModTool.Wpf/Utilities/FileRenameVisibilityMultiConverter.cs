using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PIModTool.Wpf.Utilities
{
    public class FileRenameVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Collapsed;

            var fileBeingRenamed = values[0];
            var currentItem = values[1];
            var mode = parameter as string;

            bool isRenaming = fileBeingRenamed == currentItem;

            return (mode == "Edit" && isRenaming) || (mode == "Display" && !isRenaming)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
