using Pfim;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PIModTool.Wpf.Utilities
{
    public class BinaryToBitmapConverter: IValueConverter
    {
        private static PixelFormat PfimFormatToWpf(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.Rgba32 => PixelFormats.Bgra32,
                ImageFormat.Rgb24 => PixelFormats.Bgr24,
                ImageFormat.R5g5b5 => PixelFormats.Bgr555,
                ImageFormat.R5g6b5 => PixelFormats.Bgr565,
                ImageFormat.Rgb8 => PixelFormats.Gray8,
                _ => throw new NotSupportedException($"Unsupported Pfim format: {format}")
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not byte[] data || data.Length == 0)
            {
                return null;
            }
            try
            {
                MemoryStream stream = new MemoryStream(data);
                using IImage image = Pfimage.FromStream(stream);

                PixelFormat pixelFormat = PfimFormatToWpf(image.Format);
                int stride = (image.Width * pixelFormat.BitsPerPixel + 7) / 8;

                return BitmapSource.Create(image.Width, image.Height, 96, 96, pixelFormat, null, image.Data, stride);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported.");
        }
    }
}
