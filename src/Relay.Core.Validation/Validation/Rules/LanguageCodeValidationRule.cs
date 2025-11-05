using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid ISO 639 language code.
    /// Supports both 2-letter and 3-letter language codes.
    /// </summary>
    public class LanguageCodeValidationRule : IValidationRule<string>
    {
        private static readonly HashSet<string> ValidLanguageCodes = new HashSet<string>
        {
            // ISO 639-1 (2-letter codes)
            "aa", "ab", "ae", "af", "ak", "am", "an", "ar", "as", "av",
            "ay", "az", "ba", "be", "bg", "bh", "bi", "bm", "bn", "bo",
            "br", "bs", "ca", "ce", "ch", "co", "cr", "cs", "cu", "cv",
            "cy", "da", "de", "dv", "dz", "ee", "el", "en", "eo", "es",
            "et", "eu", "fa", "ff", "fi", "fj", "fo", "fr", "fy", "ga",
            "gd", "gl", "gn", "gu", "gv", "ha", "he", "hi", "ho", "hr",
            "ht", "hu", "hy", "hz", "ia", "id", "ie", "ig", "ii", "ik",
            "io", "is", "it", "iu", "ja", "jv", "ka", "kg", "ki", "kj",
            "kk", "kl", "km", "kn", "ko", "kr", "ks", "ku", "kv", "kw",
            "ky", "la", "lb", "lg", "li", "ln", "lo", "lt", "lu", "lv",
            "mg", "mh", "mi", "mk", "ml", "mn", "mr", "ms", "mt", "my",
            "na", "nb", "nd", "ne", "ng", "nl", "nn", "no", "nr", "nv",
            "ny", "oc", "oj", "om", "or", "os", "pa", "pi", "pl", "ps",
            "pt", "qu", "rm", "rn", "ro", "ru", "rw", "sa", "sc", "sd",
            "se", "sg", "si", "sk", "sl", "sm", "sn", "so", "sq", "sr",
            "ss", "st", "su", "sv", "sw", "ta", "te", "tg", "th", "ti",
            "tk", "tl", "tn", "to", "tr", "ts", "tt", "tw", "ty", "ug",
            "uk", "ur", "uz", "ve", "vi", "vo", "wa", "wo", "xh", "yi",
            "yo", "za", "zh", "zu",

            // ISO 639-3 (3-letter codes) - subset of common ones
            "aar", "abk", "ave", "afr", "aka", "amh", "arg", "ara", "asm", "ava",
            "aym", "aze", "bak", "bel", "bul", "bih", "bis", "bam", "ben", "tib",
            "bre", "bos", "cat", "che", "cha", "cos", "cre", "cze", "chu", "chv",
            "wel", "dan", "ger", "div", "dzo", "ewe", "gre", "eng", "epo", "spa",
            "est", "eus", "per", "ful", "fin", "fij", "fao", "fre", "fry", "gle",
            "gla", "glg", "grn", "guj", "glv", "hau", "heb", "hin", "hmo", "hrv",
            "hat", "hun", "arm", "her", "ina", "ind", "ile", "ibo", "iii", "ipk",
            "ido", "ice", "ita", "iku", "jpn", "jav", "geo", "kon", "kik", "kua",
            "kaz", "kal", "khm", "kan", "kor", "kan", "kas", "kur", "kom", "cor",
            "kir", "lat", "ltz", "lug", "lim", "lin", "lao", "lit", "lub", "lav",
            "mlg", "mah", "mao", "mac", "mal", "mon", "mar", "may", "mlt", "bur",
            "nau", "nob", "nde", "nep", "ndo", "dut", "nno", "nor", "nbl", "nav",
            "nya", "oci", "oji", "orm", "ori", "oss", "pan", "pli", "pol", "pus",
            "por", "que", "roh", "run", "rum", "rus", "kin", "san", "srd", "snd",
            "sme", "sag", "sin", "slo", "slv", "smo", "sna", "som", "alb", "srp",
            "ssw", "sot", "sun", "swe", "swa", "tam", "tel", "tgk", "tha", "tir",
            "tuk", "tgl", "tsn", "ton", "tur", "tso", "tat", "twi", "tah", "uig",
            "ukr", "urd", "uzb", "ven", "vie", "vol", "wln", "wol", "xho", "yid",
            "yor", "zha", "chi", "zul"
        };

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var code = request.ToLowerInvariant().Trim();
            if ((code.Length != 2 && code.Length != 3) || !ValidLanguageCodes.Contains(code))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid ISO 639 language code." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}