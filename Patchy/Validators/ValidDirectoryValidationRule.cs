using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Patchy.Validators
{
    public class ValidDirectoryValidationRule : ValidationRule
    {
        public ValidDirectoryValidationRule()
        {
            AllowEmpty = true;
        }

        public bool AllowEmpty { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var path = value as string;
            if (path == null || !Directory.Exists(path))
            {
                if (string.IsNullOrEmpty(path) && AllowEmpty)
                    return new ValidationResult(true, null);
                return new ValidationResult(false, "The specified path does not exist.");
            }
            return new ValidationResult(true, null);
        }
    }
}
