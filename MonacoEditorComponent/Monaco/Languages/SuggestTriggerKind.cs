namespace Monaco.Languages
{
    public enum CompletionTriggerKind
    {
        Invoke = 0,
        TriggerCharacter = 1,
        TriggerForIncompleteCompletions = 2
    }

    public enum SignatureHelpTriggerKind
    {
        ContentChange = 3,
        Invoke = 1,
        TriggerCharacter = 2
    }
}