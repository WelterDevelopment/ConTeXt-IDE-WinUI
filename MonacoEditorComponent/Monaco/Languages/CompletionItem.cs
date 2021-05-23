using Monaco.Editor;
using Newtonsoft.Json;

namespace Monaco.Languages
{
    /// <summary>
    /// A completion item represents a text snippet that is
    /// proposed to complete text that is being typed.
    /// </summary>
    public sealed class CompletionItem
    {
        [JsonProperty("additionalTextEdits", NullValueHandling = NullValueHandling.Ignore)]
        public ISingleEditOperation[] AdditionalTextEdits { get; set; }

        [JsonProperty("command", NullValueHandling = NullValueHandling.Ignore)]
        public Command Command { get; set; }

        [JsonProperty("commitCharacters", NullValueHandling = NullValueHandling.Ignore)]
        public string[] CommitCharacters { get; set; }

        [JsonProperty("detail", NullValueHandling = NullValueHandling.Ignore)]
        public string Detail { get; set; }

        [JsonProperty("documentation", NullValueHandling = NullValueHandling.Ignore)]
        public IMarkdownString Documentation { get; set; }

        [JsonProperty("filterText", NullValueHandling = NullValueHandling.Ignore)]
        public string FilterText { get; set; }

        [JsonProperty("insertText")]
        public string InsertText { get; set; }

        [JsonProperty("insertTextRules", NullValueHandling = NullValueHandling.Ignore)]
        public CompletionItemInsertTextRule? InsertTextRules { get; set; }

        [JsonProperty("kind")]
        public CompletionItemKind Kind { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("preselect", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Preselect { get; set; }

        [JsonProperty("range")]
        public Range Range { get; set; }

        [JsonProperty("sortText", NullValueHandling = NullValueHandling.Ignore)]
        public string SortText { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public MarkerTag[] Tags { get; set; }

        public CompletionItem(string label, string insertText = null, CompletionItemKind kind = CompletionItemKind.Property)
        {
            InsertText = insertText ?? label;
            Label = label;
            Kind = kind;
        }
    }

    public sealed class SignatureHelp
    {
        [JsonProperty("activeParameter", NullValueHandling = NullValueHandling.Ignore)]
        public int ActiveParameter { get; set; }

        [JsonProperty("activeSignature", NullValueHandling = NullValueHandling.Ignore)]
        public int ActiveSignature { get; set; }

        [JsonProperty("signatures", NullValueHandling = NullValueHandling.Ignore)]
        public SignatureInformation[] Signatures { get; set; }

        public SignatureHelp(int activeSignature, int activeParameter)
        {
            ActiveParameter = activeParameter;
            ActiveSignature = activeSignature;
        }
    }

    public sealed class SignatureInformation
    {
        [JsonProperty("activeParameter", NullValueHandling = NullValueHandling.Ignore)]
        public int ActiveParameter { get; set; }

        [JsonProperty("documentation", NullValueHandling = NullValueHandling.Ignore)]
        public IMarkdownString Documentation { get; set; }

        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public ParameterInformation[] Parameters { get; set; }

        public SignatureInformation(int activeParameter, string label)
        {
            Label = label;
            ActiveParameter = activeParameter;
        }
    }

    public sealed class ParameterInformation
    {

        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        [JsonProperty("documentation", NullValueHandling = NullValueHandling.Ignore)]
        public IMarkdownString Documentation { get; set; }

        public ParameterInformation(string label, IMarkdownString documentation = null)
        {
            Label = label;
            Documentation = documentation;
        }
    }
}
