using System;
using System.ComponentModel;
using System.Globalization;

namespace DataGridSample
{
    public class FilterDescriptor : INotifyPropertyChanged
    {
        string? _rawText;

        public string PropertyName { get; }

        public string? RawText
        {
            get => _rawText;
            set
            {
                if (_rawText == value) return;
                _rawText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RawText)));
            }
        }

        public FilterDescriptor(string propertyName)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsActive => !string.IsNullOrWhiteSpace(RawText);

        public bool Matches(object item)
        {
            if (!IsActive) return true;

            var prop = item.GetType().GetProperty(PropertyName);
            var val = prop?.GetValue(item);

            // Try numeric path first
            if (TryGetNumeric(val, out var cellNum) && TryParseNumber(RawText!, out var filterNum))
            {
                return EvaluateNumericFilter(cellNum, RawText!);
            }

            // If raw text looks like a numeric expression (operators or 0x prefix) but cell isn't numeric, try parsing both as numbers
            if (TryParseNumber(RawText!, out var parsed) && TryParseNumber(val?.ToString() ?? string.Empty, out var parsedCell))
            {
                return EvaluateNumericFilter(parsedCell, RawText!);
            }

            // fallback to contains string match
            var s = val?.ToString();
            return s?.IndexOf(RawText!, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool TryGetNumeric(object? val, out long result)
        {
            result = 0;
            if (val == null) return false;
            switch (val)
            {
                case byte b: result = b; return true;
                case sbyte sb: result = sb; return true;
                case short s: result = s; return true;
                case ushort us: result = us; return true;
                case int i: result = i; return true;
                case uint ui: result = ui; return true;
                case long l: result = l; return true;
                case ulong ul when ul <= long.MaxValue: result = (long)ul; return true;
                case string str:
                    return TryParseNumber(str, out result);
                default:
                    try
                    {
                        var converted = Convert.ToInt64(val, CultureInfo.InvariantCulture);
                        result = converted;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }
        }

        // Accepts decimal or 0x-prefixed hex
        static bool TryParseNumber(string text, out long value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.Trim();
            // Range or operator expressions are not a single number; return false so caller can handle
            if (t.Contains("..") || t.StartsWith(">") || t.StartsWith("<") || t.StartsWith("="))
                return false;
            if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(t.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            }
            return long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        // Supports: n..m, >n, >=n, <n, <=n, =n, single value n
        static bool EvaluateNumericFilter(long cellValue, string filter)
        {
            var t = filter.Trim();
            // range
            var idx = t.IndexOf("..", StringComparison.Ordinal);
            if (idx >= 0)
            {
                var a = t.Substring(0, idx).Trim();
                var b = t.Substring(idx + 2).Trim();
                if (TryParseAnyNumber(a, out var lo) && TryParseAnyNumber(b, out var hi))
                    return cellValue >= lo && cellValue <= hi;
                return false;
            }

            // operators
            if (t.StartsWith(">=", StringComparison.Ordinal))
            {
                if (TryParseAnyNumber(t.Substring(2), out var v)) return cellValue >= v; return false;
            }
            if (t.StartsWith("<=", StringComparison.Ordinal))
            {
                if (TryParseAnyNumber(t.Substring(2), out var v)) return cellValue <= v; return false;
            }
            if (t.StartsWith(">", StringComparison.Ordinal))
            {
                if (TryParseAnyNumber(t.Substring(1), out var v)) return cellValue > v; return false;
            }
            if (t.StartsWith("<", StringComparison.Ordinal))
            {
                if (TryParseAnyNumber(t.Substring(1), out var v)) return cellValue < v; return false;
            }
            if (t.StartsWith("=", StringComparison.Ordinal))
            {
                if (TryParseAnyNumber(t.Substring(1), out var v)) return cellValue == v; return false;
            }

            // single value
            if (TryParseAnyNumber(t, out var single)) return cellValue == single;
            return false;
        }

        static bool TryParseAnyNumber(string text, out long value)
        {
            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return long.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
