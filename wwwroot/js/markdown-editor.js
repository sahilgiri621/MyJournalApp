window.markdownEditor = {
    wrapSelection: function (id, prefix, suffix, placeholder) {
        var element = document.getElementById(id);
        if (!element) {
            return;
        }

        var start = element.selectionStart || 0;
        var end = element.selectionEnd || 0;
        var value = element.value || "";
        var selected = value.substring(start, end);

        if (!selected && placeholder) {
            selected = placeholder;
        }

        var nextValue = value.substring(0, start) + prefix + selected + suffix + value.substring(end);
        element.value = nextValue;
        element.focus();

        var cursor = start + prefix.length + selected.length;
        element.setSelectionRange(cursor, cursor);
        element.dispatchEvent(new Event("input", { bubbles: true }));
    },

    prefixLines: function (id, prefix) {
        var element = document.getElementById(id);
        if (!element) {
            return;
        }

        var start = element.selectionStart || 0;
        var end = element.selectionEnd || 0;
        var value = element.value || "";

        var before = value.substring(0, start);
        var selection = value.substring(start, end) || "";
        var after = value.substring(end);

        var lines = selection.split("\n");
        var updated = lines.map(function (line) {
            return prefix + line;
        }).join("\n");

        var nextValue = before + updated + after;
        element.value = nextValue;
        element.focus();

        var cursor = before.length + updated.length;
        element.setSelectionRange(cursor, cursor);
        element.dispatchEvent(new Event("input", { bubbles: true }));
    }
};
