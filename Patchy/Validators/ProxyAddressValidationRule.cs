using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Controls;

namespace Patchy.Validators
{
    public class ProxyAddressValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object _value, CultureInfo cultureInfo)
        {
            var value = _value as string;
            if (value == null)
                return new ValidationResult(false, "Input is not a string");
            if (value.Count(c => c == ':') > 1)
                return new ValidationResult(false, "Invalid address");
            if (value.Contains(':'))
            {
                var parts = value.Split(':');
                int temp;
                if (!int.TryParse(parts[1], out temp))
                    return new ValidationResult(false, "Invalid port numbe");
            }
            return new ValidationResult(true, null);
        }
    }
}
