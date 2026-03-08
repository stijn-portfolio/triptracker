using System.Globalization;

namespace TripTracker.App.Converters
{
    // Converter om te checken of een jaar geselecteerd is
    // Retourneert BackgroundColor voor jaar filter buttons
    public class YearSelectedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value = SelectedYear (int? van ViewModel)
            // parameter = jaar van de button (int of null voor "All")

            int? selectedYear = value as int?;
            int? buttonYear = parameter as int?;

            // Check of deze button actief is
            bool isSelected = selectedYear == buttonYear;

            // Return kleur: paars voor actief, lichtgrijs voor inactief
            return isSelected ? Color.FromArgb("#512BD4") : Color.FromArgb("#E0E0E0");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter voor TextColor van jaar buttons
    public class YearSelectedTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int? selectedYear = value as int?;
            int? buttonYear = parameter as int?;

            bool isSelected = selectedYear == buttonYear;

            // Wit voor actief, zwart voor inactief
            return isSelected ? Colors.White : Colors.Black;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
