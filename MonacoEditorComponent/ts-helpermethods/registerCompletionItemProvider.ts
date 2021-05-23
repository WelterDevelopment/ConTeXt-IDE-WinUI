///<reference path="../monaco-editor/monaco.d.ts" />
declare var Parent: ParentAccessor;

var registerCompletionItemProvider = function (languageId, characters) {
    return monaco.languages.registerCompletionItemProvider(languageId, {
        triggerCharacters: characters,
        provideCompletionItems: function (model, position, context, token) {
            return Parent.callEvent("CompletionItemProvider" + languageId, [JSON.stringify(position), JSON.stringify(context)]).then(result => {
                if (result) {
                    return JSON.parse(result);
                }
            });
        },
        resolveCompletionItem: function (model, position, item, token) {
            return Parent.callEvent("CompletionItemRequested" + languageId, [JSON.stringify(position), JSON.stringify(item)]).then(result => {
                if (result) {
                    return JSON.parse(result);
                }
            });
        }
    });
}
var registerSignatureHelpProvider = function (languageId, characters, recharacters) {
    return monaco.languages.registerSignatureHelpProvider(languageId, {
        signatureHelpTriggerCharacters: characters,
        signatureHelpRetriggerCharacters: recharacters,
        provideSignatureHelp: function (model, position, token, context) {
            return Parent.callEvent("SignatureHelpProvider" + languageId, [JSON.stringify(position), JSON.stringify(context)]).then(result => {
                if (result) {
                    return JSON.parse(result);
                }
            });
        }
    });
}