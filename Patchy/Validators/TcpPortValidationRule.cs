using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Patchy.Validators
{
    public class TcpPortValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                int asInt = Convert.ToInt32(value);
                return new ValidationResult(asInt >= 0 && asInt <= 0xFFFF, "Please specify a number between 0 and 65535");
            }
            catch
            {
                return new ValidationResult(false, "Please specify a number between 0 and 65535");
            }
        }
    }
}
