var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var registerHoverProvider = function (languageId) {
    return monaco.languages.registerHoverProvider(languageId, {
        provideHover: function (model, position) {
            return Parent.callEvent("HoverProvider" + languageId, [JSON.stringify(position)]).then(result => {
                if (result) {
                    return JSON.parse(result);
                }
            });
        }
    });
};
var addAction = function (action) {
    action.run = function (ed) {
        Parent.callAction("Action" + action.id);
    };
    editor.addAction(action);
};
var addCommand = function (keybindingStr, handlerName, context) {
    return editor.addCommand(parseInt(keybindingStr), () => {
        Parent.callAction(handlerName);
    }, context);
};
var createContext = function (context) {
    if (context) {
        contexts[context.key] = editor.createContextKey(context.key, context.defaultValue);
    }
};
var updateContext = function (key, value) {
    contexts[key].set(value);
};
var updateContent = function (content) {
    if (content != model.getValue()) {
        model.setValue(content);
    }
};
var updateDecorations = function (newHighlights) {
    if (newHighlights) {
        decorations = editor.deltaDecorations(decorations, newHighlights);
    }
    else {
        decorations = editor.deltaDecorations(decorations, []);
    }
};
var updateStyle = function (innerStyle) {
    var style = document.getElementById("dynamic");
    style.innerHTML = innerStyle;
};
var getOptions = function () {
    return __awaiter(this, void 0, void 0, function* () {
        let opt = null;
        try {
            opt = JSON.parse(yield Parent.getJsonValue("Options"));
        }
        finally {
        }
        if (opt != null && typeof opt === "object") {
            return opt;
        }
        return {};
    });
};
var updateOptions = function (opt) {
    if (opt != null && typeof opt === "object") {
        editor.updateOptions(opt);
    }
};
var updateLanguage = function (language) {
    monaco.editor.setModelLanguage(model, language);
};
var changeTheme = function (theme, highcontrast) {
    var newTheme = 'vs';
    if (highcontrast == "True" || highcontrast == "true") {
        newTheme = 'hc-black';
    }
    else if (theme == "Dark") {
        newTheme = 'vs-dark';
    }
    monaco.editor.setTheme(newTheme);
};
var keyDown = function (event) {
    return __awaiter(this, void 0, void 0, function* () {
        var result = yield Keyboard.keyDown(event.keyCode, event.ctrlKey, event.shiftKey, event.altKey, event.metaKey);
        if (result) {
            event.cancelBubble = true;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            return false;
        }
    });
};
