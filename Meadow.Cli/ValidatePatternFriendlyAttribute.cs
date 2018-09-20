using System;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Meadow.Cli
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ValidatePatternFriendlyAttribute : ValidateEnumeratedArgumentsAttribute
    {
#pragma warning disable CA1307 // Specify StringComparison
        private string Name { get; } = nameof(ValidatePatternFriendlyAttribute).Replace("Attribute", "");
#pragma warning restore CA1307 // Specify StringComparison
        public RegexOptions Options { get; set; } = RegexOptions.IgnoreCase;
        public string Message { get; set; }
        public string Pattern { get; set; }

        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                $"{Message}\n{Name} failure: argument is null");
            }

            if (!new Regex(Pattern, Options).Match(element.ToString()).Success)
            {
                throw new ValidationMetadataException(
                $"{Message}\n{Name} failure, the value '{element}' does not match the pattern /{Pattern}/");
            }
        }
    }
}
