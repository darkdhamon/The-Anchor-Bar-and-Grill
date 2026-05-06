(function () {
  const themeKey = "anchor-theme";
  const lightTheme = "light";
  const darkTheme = "dark";

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

  function initialize() {
    applyTheme(getStoredTheme() ?? lightTheme);

    document.querySelectorAll(".switch input[type='checkbox']").forEach((input) => {
      input.removeEventListener("change", handleThemeToggle);
      input.addEventListener("change", handleThemeToggle);
    });
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
})();
