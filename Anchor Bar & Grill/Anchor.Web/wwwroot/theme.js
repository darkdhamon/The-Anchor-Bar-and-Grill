(function () {
  const themeKey = "anchor-theme";
  const lightTheme = "light";
  const darkTheme = "dark";
  const headerMenuSelector = ".site-header__nav-stack";
  const menuSectionSelectors = [".site-navigation", ".preview-nav"];
  const imagePreviewOpenClass = "image-preview-open";
  let activeImagePreview = null;

  function getCookieTheme() {
    const match = document.cookie.match(/(?:^|; )anchor-theme=([^;]+)/);
    return match ? decodeURIComponent(match[1]) : null;
  }

  function getStoredTheme() {
    try {
      const localTheme = window.localStorage.getItem(themeKey);
      if (localTheme === lightTheme || localTheme === darkTheme) {
        return localTheme;
      }
    } catch {
      // Ignore storage access issues and fall back to cookies.
    }

    const cookieTheme = getCookieTheme();
    return cookieTheme === darkTheme ? darkTheme : cookieTheme === lightTheme ? lightTheme : null;
  }

  function getSystemTheme() {
    if (!window.matchMedia) {
      return null;
    }

    if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
      return darkTheme;
    }

    if (window.matchMedia("(prefers-color-scheme: light)").matches) {
      return lightTheme;
    }

    return null;
  }

  function getTimeOfDayTheme() {
    const currentHour = new Date().getHours();
    return currentHour >= 18 || currentHour < 6 ? darkTheme : lightTheme;
  }

  function resolveTheme() {
    return getStoredTheme() ?? getSystemTheme() ?? getTimeOfDayTheme();
  }

  function getThemeShell() {
    return document.querySelector(".site-shell");
  }

  function syncRootTheme(theme) {
    document.documentElement.classList.remove("anchor-theme-light", "anchor-theme-dark");
    document.documentElement.classList.add(theme === darkTheme ? "anchor-theme-dark" : "anchor-theme-light");
  }

  function syncToggleInputs(isDark) {
    document.querySelectorAll(".switch input[type='checkbox']").forEach((input) => {
      input.checked = isDark;
    });
  }

  function applyTheme(theme) {
    syncRootTheme(theme);

    const siteShell = getThemeShell();

    if (siteShell) {
      siteShell.classList.remove("theme-light", "theme-dark");
      siteShell.classList.add(theme === darkTheme ? "theme-dark" : "theme-light");
    }

    syncToggleInputs(theme === darkTheme);
  }

  function persistTheme(theme) {
    try {
      window.localStorage.setItem(themeKey, theme);
    } catch {
      // Ignore storage access issues and still persist with a cookie.
    }

    document.cookie = "anchor-theme=" + encodeURIComponent(theme) + "; path=/; max-age=31536000; samesite=lax";
  }

  function setTheme(isDark) {
    const theme = isDark ? darkTheme : lightTheme;
    persistTheme(theme);
    applyTheme(theme);
  }

  function handleThemeToggle(event) {
    setTheme(Boolean(event.target.checked));
  }

  function handleDocumentChange(event) {
    if (event.target.matches(".switch input[type='checkbox'][data-anchor-theme-toggle='true']")) {
      handleThemeToggle(event);
    }
  }

  function getHeaderMenu(button) {
    const targetId = button?.getAttribute("data-anchor-menu-target");

    if (!targetId) {
      return null;
    }

    return document.getElementById(targetId);
  }

  function syncHeaderMenu(button, isOpen) {
    const menu = getHeaderMenu(button);

    if (!menu) {
      return;
    }

    menu.classList.toggle("is-open", isOpen);

    menuSectionSelectors.forEach((selector) => {
      menu.querySelectorAll(selector).forEach((element) => {
        element.classList.toggle("is-open", isOpen);
      });
    });

    button.setAttribute("aria-expanded", isOpen ? "true" : "false");
  }

  function syncHeaderHeight() {
    const header = document.querySelector(".site-header");
    const height = header ? Math.round(header.getBoundingClientRect().height) : 0;
    document.documentElement.style.setProperty("--anchor-header-height", `${height}px`);
  }

  function closeHeaderMenus() {
    document.querySelectorAll("[data-anchor-menu-toggle='true']").forEach((button) => {
      syncHeaderMenu(button, false);
    });

    syncHeaderHeight();
  }

  function getImagePreviewModal(targetId) {
    if (!targetId) {
      return null;
    }

    return document.getElementById(targetId);
  }

  function closeImagePreview(modal = activeImagePreview) {
    if (!modal) {
      return;
    }

    modal.hidden = true;

    if (modal === activeImagePreview) {
      activeImagePreview = null;
    }

    document.body.classList.remove(imagePreviewOpenClass);
  }

  function openImagePreview(modal) {
    if (!modal) {
      return;
    }

    if (activeImagePreview && activeImagePreview !== modal) {
      closeImagePreview(activeImagePreview);
    }

    activeImagePreview = modal;
    modal.hidden = false;
    document.body.classList.add(imagePreviewOpenClass);

    const dialog = modal.querySelector(".image-preview-modal");

    if (dialog instanceof HTMLElement) {
      dialog.focus();
    }
  }

  function handleDocumentClick(event) {
    const imagePreviewTrigger = event.target.closest("[data-image-preview-open]");

    if (imagePreviewTrigger) {
      event.preventDefault();
      openImagePreview(getImagePreviewModal(imagePreviewTrigger.getAttribute("data-image-preview-open")));
      return;
    }

    if (event.target.closest("[data-image-preview-close]")) {
      event.preventDefault();
      closeImagePreview(event.target.closest("[data-image-preview-modal]"));
      return;
    }

    if (event.target.matches("[data-image-preview-modal]")) {
      event.preventDefault();
      closeImagePreview(event.target);
      return;
    }

    const menuToggle = event.target.closest("[data-anchor-menu-toggle='true']");

    if (menuToggle) {
      event.preventDefault();
      const isExpanded = menuToggle.getAttribute("aria-expanded") === "true";
      syncHeaderMenu(menuToggle, !isExpanded);
      syncHeaderHeight();
      return;
    }

    if (event.target.closest(`${headerMenuSelector} a, ${headerMenuSelector} button[type='submit']`)) {
      closeHeaderMenus();
      return;
    }

    if (!event.target.closest(".site-header")) {
      closeHeaderMenus();
    }
  }

  function handleDocumentKeyDown(event) {
    if (event.key === "Escape" && activeImagePreview) {
      closeImagePreview(activeImagePreview);
    }
  }

  function initialize() {
    applyTheme(resolveTheme());
    closeHeaderMenus();
    syncHeaderHeight();
  }

  window.anchorTheme = {
    initialize,
    setTheme
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }

  document.removeEventListener("change", handleDocumentChange);
  document.addEventListener("change", handleDocumentChange);
  document.removeEventListener("click", handleDocumentClick);
  document.addEventListener("click", handleDocumentClick);
  document.removeEventListener("keydown", handleDocumentKeyDown);
  document.addEventListener("keydown", handleDocumentKeyDown);
  window.removeEventListener("resize", syncHeaderHeight);
  window.addEventListener("resize", syncHeaderHeight);
})();
