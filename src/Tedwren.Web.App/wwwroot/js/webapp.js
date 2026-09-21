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
        }
    };
})();
