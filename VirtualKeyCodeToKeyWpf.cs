using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace KeysSendingApplication2
{
    public class VirtualKeyCodeToKeyWpf : IValueConverter
    {
        public VirtualKeyCodeToKeyWpf()
        {
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var byteValue = (byte) value;
                if (byteValue == 0)
                    return "None";

                var key = KeyInterop.KeyFromVirtualKey(byteValue);
                return Enum.GetName(typeof(Key), key);
            }
            catch (InvalidCastException)
            {
                return "Invalid cast";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            if (stringValue == null)
                return (byte) 0;
            try
            {
                var converter = new KeyConverter();
                var key = (Key)converter.ConvertFromString(stringValue);
                return (byte)KeyInterop.VirtualKeyFromKey(key);
            }
            catch (NotSupportedException)
            {
                return (byte)0;
            }
        }
    }
}
