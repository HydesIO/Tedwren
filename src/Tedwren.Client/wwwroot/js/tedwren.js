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

    auth: {
        get: function () {
            try { return localStorage.getItem('tedwren-auth') || ''; }
            catch (e) { return ''; }
        },
        set: function (value) {
            try { localStorage.setItem('tedwren-auth', value); } catch (e) { /* ignore */ }
        },
        clear: function () {
            try { localStorage.removeItem('tedwren-auth'); } catch (e) { /* ignore */ }
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
    },

    // Triggers a browser download of base64 content (e.g. a captured form file).
    download: function (fileName, base64, contentType) {
        try {
            var byteChars = atob(base64);
            var bytes = new Uint8Array(byteChars.length);
            for (var i = 0; i < byteChars.length; i++) { bytes[i] = byteChars.charCodeAt(i); }
            var blob = new Blob([bytes], { type: contentType || 'application/octet-stream' });
            var url = URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = fileName || 'download';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
        } catch (e) { /* ignore */ }
    },

    // Canvas signature pad: pointer-drawn strokes captured as a PNG data URL (MC-5 style signature).
    signature: {
        init: function (canvas, dotNetRef) {
            if (!canvas) { return; }
            var ctx = canvas.getContext('2d');
            // Match the backing store to the displayed size for crisp lines.
            var rect = canvas.getBoundingClientRect();
            var ratio = window.devicePixelRatio || 1;
            canvas.width = Math.max(1, Math.round(rect.width * ratio));
            canvas.height = Math.max(1, Math.round(rect.height * ratio));
            ctx.scale(ratio, ratio);
            ctx.lineWidth = 2;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
            ctx.strokeStyle = '#101828';

            var drawing = false;
            var dirty = false;

            function pos(e) {
                var r = canvas.getBoundingClientRect();
                return { x: e.clientX - r.left, y: e.clientY - r.top };
            }
            function start(e) {
                drawing = true; dirty = true;
                var p = pos(e);
                ctx.beginPath();
                ctx.moveTo(p.x, p.y);
                e.preventDefault();
            }
            function move(e) {
                if (!drawing) { return; }
                var p = pos(e);
                ctx.lineTo(p.x, p.y);
                ctx.stroke();
                e.preventDefault();
            }
            function end() {
                if (!drawing) { return; }
                drawing = false;
                // Push the current signature back to .NET so the bound Value stays current.
                if (dotNetRef && dirty) {
                    try { dotNetRef.invokeMethodAsync('OnStrokeEnd', canvas.toDataURL('image/png')); }
                    catch (e) { /* circuit gone */ }
                }
            }

            canvas.addEventListener('pointerdown', start);
            canvas.addEventListener('pointermove', move);
            window.addEventListener('pointerup', end);

            canvas._tedwrenSig = {
                ctx: ctx,
                isDirty: function () { return dirty; },
                clear: function () {
                    ctx.clearRect(0, 0, canvas.width, canvas.height);
                    dirty = false;
                },
                dispose: function () {
                    canvas.removeEventListener('pointerdown', start);
                    canvas.removeEventListener('pointermove', move);
                    window.removeEventListener('pointerup', end);
                }
            };
        },
        isEmpty: function (canvas) {
            return !(canvas && canvas._tedwrenSig && canvas._tedwrenSig.isDirty());
        },
        toDataUrl: function (canvas) {
            if (!canvas || !canvas._tedwrenSig || !canvas._tedwrenSig.isDirty()) { return ''; }
            return canvas.toDataURL('image/png');
        },
        clear: function (canvas) {
            if (canvas && canvas._tedwrenSig) { canvas._tedwrenSig.clear(); }
        },
        dispose: function (canvas) {
            if (canvas && canvas._tedwrenSig) { canvas._tedwrenSig.dispose(); canvas._tedwrenSig = null; }
        }
    }
};
