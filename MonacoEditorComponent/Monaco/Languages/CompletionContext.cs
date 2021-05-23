using Newtonsoft.Json;

namespace Monaco.Languages
{
    public sealed class CompletionContext
    {
        [JsonProperty("triggerCharacter", NullValueHandling = NullValueHandling.Ignore)]
        public string TriggerCharacter { get; set; }

        [JsonProperty("triggerKind")]
        public CompletionTriggerKind TriggerKind { get; set; }
    }

    public sealed class SignatureHelpContext
    {
        [JsonProperty("triggerCharacter", NullValueHandling = NullValueHandling.Ignore)]
        public string TriggerCharacter { get; set; }

        [JsonProperty("isReTrigger", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsReTrigger { get; set; }

        [JsonProperty("activeSignatureHelp", NullValueHandling = NullValueHandling.Ignore)]
        public SignatureHelp ActiveSignatureHelp { get; set; }

        [JsonProperty("triggerKind")]
        public SignatureHelpTriggerKind TriggerKind { get; set; }
    }
}
