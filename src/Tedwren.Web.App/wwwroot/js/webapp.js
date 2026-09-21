// Emulator JS interop helpers. Kept deliberately small — the emulator's logic lives in .NET (Tedwren.Mobile.Core);
// this only bridges to browser APIs the native app gets from the OS (geolocation, connectivity, canvas).
window.webapp = (function () {
    "use strict";

    return {
        // Resolves the current position as { latitude, longitude, accuracyMetres } or null on denial/absence.
        // Mirrors the device's location capture; the caller falls back to a demo site-centre when this is null.
        getPosition: function () {
            return new Promise(function (resolve) {
                if (!navigator.geolocation) {
                    resolve(null);
                    return;
                }
                navigator.geolocation.getCurrentPosition(
                    function (pos) {
                        resolve({
                            latitude: pos.coords.latitude,
                            longitude: pos.coords.longitude,
                            accuracyMetres: pos.coords.accuracy
                        });
                    },
                    function () { resolve(null); },
                    { enableHighAccuracy: true, timeout: 8000, maximumAge: 0 }
                );
            });
        },

        // Current online state (navigator.onLine).
        isOnline: function () {
            return navigator.onLine !== false;
        },

        // Registers window online/offline listeners that call back into .NET (used by WA6's offline emulation).
        registerConnectivity: function (dotNetRef) {
            var notify = function () {
                try { dotNetRef.invokeMethodAsync("OnConnectivityChanged", navigator.onLine !== false); } catch (e) { /* best-effort */ }
            };
            window.addEventListener("online", notify);
            window.addEventListener("offline", notify);
        },

        // Canvas signature pad: draw with pointer/touch and report the PNG data URL to .NET after each stroke.
        // The web mirror of the native TwSignaturePad; the fill page stores the data URL as the field's answer.
        signature: {
            attach: function (canvas, dotNetRef) {
                if (!canvas) { return; }
                // Size the backing store to the laid-out element for a crisp line.
                canvas.width = canvas.clientWidth || 320;
                canvas.height = canvas.clientHeight || 140;
                var ctx = canvas.getContext("2d");
                var drawing = false, last = null;
                function pos(e) {
                    var r = canvas.getBoundingClientRect();
                    var p = (e.touches && e.touches[0]) || e;
                    return { x: p.clientX - r.left, y: p.clientY - r.top };
                }
                function start(e) { drawing = true; last = pos(e); e.preventDefault(); }
                function move(e) {
                    if (!drawing) { return; }
                    var p = pos(e);
                    ctx.strokeStyle = getComputedStyle(canvas).color || "#101828";
                    ctx.lineWidth = 2; ctx.lineCap = "round";
                    ctx.beginPath(); ctx.moveTo(last.x, last.y); ctx.lineTo(p.x, p.y); ctx.stroke();
                    last = p; e.preventDefault();
                }
                function end() {
                    if (!drawing) { return; }
                    drawing = false;
                    if (dotNetRef) { try { dotNetRef.invokeMethodAsync("OnStrokeEnd", canvas.toDataURL("image/png")); } catch (_) { /* best-effort */ } }
                }
                canvas.addEventListener("pointerdown", start);
                canvas.addEventListener("pointermove", move);
                window.addEventListener("pointerup", end);
            },
            clear: function (canvas) {
                if (!canvas) { return; }
                var ctx = canvas.getContext("2d");
                ctx.clearRect(0, 0, canvas.width, canvas.height);
            }
        }
    };
})();
