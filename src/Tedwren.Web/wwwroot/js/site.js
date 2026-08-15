/*
 * Tedwren.Web progressive enhancement. Dependency-free, deferred. Every page is fully usable with this
 * script absent or disabled: tabs fall back to all panels visible, the live clock is decorative, and
 * reveal animations are skipped. All motion respects prefers-reduced-motion.
 */
(function () {
  "use strict";

  /* ---- Audience toggle + product tabs (mirrored) ------------------------------------------------ */
  // A single choice ("sub" | "main") drives both the hero audience control and the product tab panels.
  function selectAudience(which) {
    document.querySelectorAll("[data-audience-btn]").forEach(function (btn) {
      var active = btn.getAttribute("data-audience-btn") === which;
      btn.classList.toggle("is-active", active);
      btn.setAttribute("aria-selected", active ? "true" : "false");
    });
    document.querySelectorAll("[data-audience-panel]").forEach(function (panel) {
      panel.classList.toggle("is-active", panel.getAttribute("data-audience-panel") === which);
    });
  }

  var audienceButtons = document.querySelectorAll("[data-audience-btn]");
  if (audienceButtons.length) {
    audienceButtons.forEach(function (btn) {
      btn.addEventListener("click", function () {
        selectAudience(btn.getAttribute("data-audience-btn"));
      });
    });
    // Enhance: JS present, so activate the segmented behaviour (default markup shows the first panel).
    selectAudience(document.querySelector("[data-audience-default]")
      ? document.querySelector("[data-audience-default]").getAttribute("data-audience-default")
      : "sub");
  }

  /* ---- Live clock in the register frame --------------------------------------------------------- */
  var clock = document.querySelector("[data-clock]");
  if (clock) {
    var pad = function (n) { return String(n).padStart(2, "0"); };
    var tick = function () {
      var now = new Date();
      clock.textContent = pad(now.getHours()) + ":" + pad(now.getMinutes()) + ":" + pad(now.getSeconds());
    };
    tick();
    setInterval(tick, 1000);
  }
})();
