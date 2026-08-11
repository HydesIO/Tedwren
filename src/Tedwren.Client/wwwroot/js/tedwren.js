// Small interop helpers for the Tedwren shell: theme persistence, the Ctrl/Cmd+K
// command-palette shortcut, and focusing an element.
window.tedwren = {
    theme: {
        get: function () {
            try { return localStorage.getItem('tedwren-theme') || ''; }
            catch (e) { return ''; }
        },
        set: function (value) {
            try { localStorage.setItem('tedwren-theme', value); } catch (e) { /* ignore */ }
        }
    },

    tenant: {
        get: function () {
            try { return localStorage.getItem('tedwren-company') || ''; }
            catch (e) { return ''; }
        },
        set: function (value) {
            try { localStorage.setItem('tedwren-company', value); } catch (e) { /* ignore */ }
        }
    },

    shortcuts: {
        _handler: null,
        register: function (dotnetRef) {
            window.tedwren.shortcuts.unregister();
            window.tedwren.shortcuts._handler = function (e) {
                if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
                    e.preventDefault();
                    dotnetRef.invokeMethodAsync('OnCommandKey');
                }
            };
            document.addEventListener('keydown', window.tedwren.shortcuts._handler);
        },
        unregister: function () {
            if (window.tedwren.shortcuts._handler) {
                document.removeEventListener('keydown', window.tedwren.shortcuts._handler);
                window.tedwren.shortcuts._handler = null;
            }
        }
    },

    focus: function (element) {
        if (element && typeof element.focus === 'function') {
            // Defer so the element is in the DOM and layout is settled.
            setTimeout(function () { element.focus(); }, 0);
        }
    }
};
