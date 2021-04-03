using System.Linq;
using System.Text;
using SharpGen.Config;

namespace SharpGen.Transform
{
    public sealed partial class NamingRulesManager
    {
        /// <summary>
        /// Determines whether the specified string is a valid Pascal case.
        /// </summary>
        /// <param name="str">The string to validate.</param>
        /// <param name="lowerCount">The lower count.</param>
        /// <returns>
        /// 	<c>true</c> if the specified string is a valid Pascal case; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsPascalCase(string str, out int lowerCount)
        {
            // Count the number of char in lower case
            lowerCount = str.Count(char.IsLower);

            if (str.Length == 0)
                return false;

            // First char must be a letter
            if (!char.IsLetter(str[0]))
                return false;

            // First letter must be upper
            if (!char.IsUpper(str[0]))
                return false;

            // Second letter must be lower
            if (str.Length > 1 && char.IsUpper(str[1]))
                return false;

            return str.All(char.IsLetterOrDigit);
        }

        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="namingFlags">The naming options to apply to the given string to convert.</param>
        /// <returns>The given string in PascalCase.</returns>
        public string ConvertToPascalCase(string text, NamingFlags namingFlags)
        {
            var splittedPhrase = text.Split('_');
            StringBuilder sb = new();

            for (var i = 0; i < splittedPhrase.Length; i++)
            {
                var subPart = splittedPhrase[i];

                // Don't perform expansion when asked
                if ((namingFlags & NamingFlags.NoShortNameExpand) == 0)
                {
                    while (subPart.Length > 0)
                    {
                        var continueReplace = false;
                        foreach (var regExp in _expandShortName)
                        {
                            var regex = regExp.Regex;
                            var newText = regExp.Replace;

                            if (regex.Match(subPart).Success)
                            {
                                if (regExp.HasRegexReplace)
                                {
                                    subPart = regex.Replace(subPart, regExp.Replace);
                                    sb.Append(subPart);
                                    subPart = string.Empty;
                                }
                                else
                                {
                                    subPart = regex.Replace(subPart, string.Empty);
                                    sb.Append(newText);
                                    continueReplace = true;
                                }

                                break;
                            }
                        }

                        if (!continueReplace)
                        {
                            break;
                        }
                    }
                }

                // Else, perform a standard conversion
                if (subPart.Length > 0)
                {
                    // If string is not Pascal Case, then Pascal Case it
                    if (IsPascalCase(subPart, out var numberOfCharLowercase))
                    {
                        sb.Append(subPart);
                    }
                    else
                    {
                        var splittedPhraseChars = numberOfCharLowercase > 0
                                                      ? subPart.ToCharArray()
                                                      : subPart.ToLower().ToCharArray();

                        if (splittedPhraseChars.Length > 0)
                            splittedPhraseChars[0] = char.ToUpper(splittedPhraseChars[0]);

                        sb.Append(new string(splittedPhraseChars));
                    }
                }

                if ((namingFlags & NamingFlags.KeepUnderscore) != 0 && i + 1 < splittedPhrase.Length)
                    sb.Append('_');
            }

            return sb.ToString();
        }
    }
}