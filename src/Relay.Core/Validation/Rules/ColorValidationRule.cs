using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string represents a valid color.
    /// Supports hexadecimal, RGB, RGBA, HSL, HSLA, and named colors.
    /// </summary>
    public class ColorValidationRule : IValidationRule<string>
    {
        private static readonly Regex HexColorRegex = new Regex(
            @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RgbColorRegex = new Regex(
            @"^rgb\(\s*(-?\d{1,3})\s*,\s*(-?\d{1,3})\s*,\s*(-?\d{1,3})\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RgbaColorRegex = new Regex(
            @"^rgba\(\s*(-?\d{1,3})\s*,\s*(-?\d{1,3})\s*,\s*(-?\d{1,3})\s*,\s*(-?\d*\.?\d+)\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HslColorRegex = new Regex(
            @"^hsl\(\s*(-?\d{1,3})\s*,\s*(-?\d{1,3})%\s*,\s*(-?\d{1,3})%\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HslaColorRegex = new Regex(
            @"^hsla\(\s*(-?\d{1,3})\s*,\s*(-?\d{1,3})%\s*,\s*(-?\d{1,3})%\s*,\s*(-?\d*\.?\d+)\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> NamedColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "aliceblue", "antiquewhite", "aqua", "aquamarine", "azure", "beige", "bisque", "black",
            "blanchedalmond", "blue", "blueviolet", "brown", "burlywood", "cadetblue", "chartreuse",
            "chocolate", "coral", "cornflowerblue", "cornsilk", "crimson", "cyan", "darkblue",
            "darkcyan", "darkgoldenrod", "darkgray", "darkgreen", "darkgrey", "darkkhaki",
            "darkmagenta", "darkolivegreen", "darkorange", "darkorchid", "darkred", "darksalmon",
            "darkseagreen", "darkslateblue", "darkslategray", "darkslategrey", "darkturquoise",
            "darkviolet", "deeppink", "deepskyblue", "dimgray", "dimgrey", "dodgerblue", "firebrick",
            "floralwhite", "forestgreen", "fuchsia", "gainsboro", "ghostwhite", "gold", "goldenrod",
            "gray", "green", "greenyellow", "grey", "honeydew", "hotpink", "indianred", "indigo",
            "ivory", "khaki", "lavender", "lavenderblush", "lawngreen", "lemonchiffon", "lightblue",
            "lightcoral", "lightcyan", "lightgoldenrodyellow", "lightgray", "lightgreen", "lightgrey",
            "lightpink", "lightsalmon", "lightseagreen", "lightskyblue", "lightslategray",
            "lightslategrey", "lightsteelblue", "lightyellow", "lime", "limegreen", "linen",
            "magenta", "maroon", "mediumaquamarine", "mediumblue", "mediumorchid", "mediumpurple",
            "mediumseagreen", "mediumslateblue", "mediumspringgreen", "mediumturquoise",
            "mediumvioletred", "midnightblue", "mintcream", "mistyrose", "moccasin", "navajowhite",
            "navy", "oldlace", "olive", "olivedrab", "orange", "orangered", "orchid", "palegoldenrod",
            "palegreen", "paleturquoise", "palevioletred", "papayawhip", "peachpuff", "peru", "pink",
            "plum", "powderblue", "purple", "rebeccapurple", "red", "rosybrown", "royalblue",
            "saddlebrown", "salmon", "sandybrown", "seagreen", "seashell", "sienna", "silver",
            "skyblue", "slateblue", "slategray", "slategrey", "snow", "springgreen", "steelblue",
            "tan", "teal", "thistle", "tomato", "turquoise", "violet", "wheat", "white",
            "whitesmoke", "yellow", "yellowgreen"
        };

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var color = request.Trim();

            // Check hexadecimal colors
            if (HexColorRegex.IsMatch(color))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            // Check RGB colors
            if (RgbColorRegex.IsMatch(color))
            {
                return ValidateRgbValues(color, RgbColorRegex);
            }

            // Check RGBA colors
            if (RgbaColorRegex.IsMatch(color))
            {
                return ValidateRgbaValues(color, RgbaColorRegex);
            }

            // Check HSL colors
            if (HslColorRegex.IsMatch(color))
            {
                return ValidateHslValues(color, HslColorRegex);
            }

            // Check HSLA colors
            if (HslaColorRegex.IsMatch(color))
            {
                return ValidateHslaValues(color, HslaColorRegex);
            }

            // Check named colors
            if (NamedColors.Contains(color.ToLowerInvariant()))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] {
                "Invalid color format. Supported formats: #RGB, #RRGGBB, #RRGGBBAA, rgb(r,g,b), rgba(r,g,b,a), hsl(h,s%,l%), hsla(h,s%,l%,a), or named colors."
            });
        }

        private static ValueTask<IEnumerable<string>> ValidateRgbValues(string color, Regex regex)
        {
            var match = regex.Match(color);
            if (!match.Success) return new ValueTask<IEnumerable<string>>(new[] { "Invalid RGB color format." });

            var r = int.Parse(match.Groups[1].Value);
            var g = int.Parse(match.Groups[2].Value);
            var b = int.Parse(match.Groups[3].Value);

            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "RGB values must be between 0 and 255." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static ValueTask<IEnumerable<string>> ValidateRgbaValues(string color, Regex regex)
        {
            var match = regex.Match(color);
            if (!match.Success) return new ValueTask<IEnumerable<string>>(new[] { "Invalid RGBA color format." });

            var r = int.Parse(match.Groups[1].Value);
            var g = int.Parse(match.Groups[2].Value);
            var b = int.Parse(match.Groups[3].Value);
            var a = double.Parse(match.Groups[4].Value);

            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "RGB values must be between 0 and 255." });
            }

            if (a < 0 || a > 1)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Alpha value must be between 0 and 1." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static ValueTask<IEnumerable<string>> ValidateHslValues(string color, Regex regex)
        {
            var match = regex.Match(color);
            if (!match.Success) return new ValueTask<IEnumerable<string>>(new[] { "Invalid HSL color format." });

            var h = int.Parse(match.Groups[1].Value);
            var s = int.Parse(match.Groups[2].Value);
            var l = int.Parse(match.Groups[3].Value);

            if (h < 0 || h > 360)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Hue must be between 0 and 360 degrees." });
            }

            if (s < 0 || s > 100 || l < 0 || l > 100)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Saturation and lightness must be between 0% and 100%." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        private static ValueTask<IEnumerable<string>> ValidateHslaValues(string color, Regex regex)
        {
            var match = regex.Match(color);
            if (!match.Success) return new ValueTask<IEnumerable<string>>(new[] { "Invalid HSLA color format." });

            var h = int.Parse(match.Groups[1].Value);
            var s = int.Parse(match.Groups[2].Value);
            var l = int.Parse(match.Groups[3].Value);
            var a = double.Parse(match.Groups[4].Value);

            if (h < 0 || h > 360)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Hue must be between 0 and 360 degrees." });
            }

            if (s < 0 || s > 100 || l < 0 || l > 100)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Saturation and lightness must be between 0% and 100%." });
            }

            if (a < 0 || a > 1)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Alpha value must be between 0 and 1." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}