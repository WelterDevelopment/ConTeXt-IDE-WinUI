using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Metadata;

namespace Monaco
{
    public sealed class IMarkdownString
    {
        [JsonProperty("isTrusted")]
        public bool IsTrusted { get; set; }
        [JsonProperty("supportThemeIcons", NullValueHandling =NullValueHandling.Ignore)]
        public bool? SupportThemeIcons { get; set; }

        [JsonProperty("uris", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Uri> Uris { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonConstructor]
        public IMarkdownString(string svalue) : this(svalue, true) { }

        public IMarkdownString(string svalue, bool isTrusted)
        {
            Value = svalue;
            IsTrusted = isTrusted;
            SupportThemeIcons = true;

        }
    }

    public static class MarkdownStringExtensions
    {
        [DefaultOverload]
        public static IMarkdownString ToMarkdownString(this string svalue)
        {
            return ToMarkdownString(svalue, false);
        }

        [DefaultOverload]
        public static IMarkdownString ToMarkdownString(this string svalue, bool isTrusted)
        {
            return new IMarkdownString(svalue, isTrusted);
        }

        public static IMarkdownString[] ToMarkdownString(this string[] values)
        {
            return ToMarkdownString(values, false);
        }

        public static IMarkdownString[] ToMarkdownString(this string[] values, bool isTrusted)
        {
            return values.Select(value => new IMarkdownString(value, isTrusted)).ToArray();
        }
    }
}
