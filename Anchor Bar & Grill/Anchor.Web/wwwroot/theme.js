(function () {
  const themeKey = "anchor-theme";
  const lightTheme = "light";
  const darkTheme = "dark";
  const headerMenuActionSelector = ".site-header__nav-stack a, .site-header__nav-stack button[type='submit'], .account-menu a, .account-menu button[type='submit']";
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
    button.setAttribute("aria-expanded", isOpen ? "true" : "false");
  }

  function syncHeaderHeight() {
    const header = document.querySelector(".site-header");
    const height = header ? Math.round(header.getBoundingClientRect().height) : 0;
    document.documentElement.style.setProperty("--anchor-header-height", `${height}px`);
  }

  function closeHeaderMenus(exceptButton = null) {
    document.querySelectorAll("[data-anchor-menu-toggle='true']").forEach((button) => {
      if (exceptButton && button === exceptButton) {
        return;
      }

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

  function initializeCarousels() {
    document.querySelectorAll("[data-anchor-carousel='true']").forEach((carousel) => {
      if (carousel.dataset.anchorCarouselInitialized === "true") {
        return;
      }

      const slides = Array.from(carousel.querySelectorAll("[data-anchor-carousel-slide]"));

      if (slides.length === 0) {
        return;
      }

      carousel.dataset.anchorCarouselInitialized = "true";

      const indicators = Array.from(carousel.querySelectorAll("[data-anchor-carousel-to]"));
      const previousButton = carousel.querySelector("[data-anchor-carousel-prev]");
      const nextButton = carousel.querySelector("[data-anchor-carousel-next]");
      const count = carousel.querySelector("[data-anchor-carousel-count]");
      const configuredInterval = Number.parseInt(carousel.getAttribute("data-anchor-carousel-interval") ?? "", 10);
      const intervalMs = Number.isFinite(configuredInterval) && configuredInterval >= 2500 ? configuredInterval : 6500;
      let activeIndex = Math.max(slides.findIndex((slide) => slide.classList.contains("is-active")), 0);
      let autoAdvanceHandle = null;
      let touchStartX = null;

      function clearAutoAdvance() {
        if (autoAdvanceHandle !== null) {
          window.clearTimeout(autoAdvanceHandle);
          autoAdvanceHandle = null;
        }
      }

      function syncCarousel() {
        slides.forEach((slide, index) => {
          const isActive = index === activeIndex;
          slide.classList.toggle("is-active", isActive);
          slide.setAttribute("aria-hidden", isActive ? "false" : "true");
        });

        indicators.forEach((indicator, index) => {
          const isActive = index === activeIndex;
          indicator.classList.toggle("is-active", isActive);
          indicator.setAttribute("aria-current", isActive ? "true" : "false");
        });

        if (count) {
          count.textContent = `${activeIndex + 1} / ${slides.length}`;
        }
      }

      function restartAutoAdvance() {
        clearAutoAdvance();

        if (slides.length < 2 || document.hidden) {
          return;
        }

        autoAdvanceHandle = window.setTimeout(() => {
          moveTo(activeIndex + 1);
        }, intervalMs);
      }

      function moveTo(nextIndex) {
        activeIndex = (nextIndex + slides.length) % slides.length;
        syncCarousel();
        restartAutoAdvance();
      }

      previousButton?.addEventListener("click", () => {
        moveTo(activeIndex - 1);
      });

      nextButton?.addEventListener("click", () => {
        moveTo(activeIndex + 1);
      });

      indicators.forEach((indicator) => {
        indicator.addEventListener("click", () => {
          const targetIndex = Number.parseInt(indicator.getAttribute("data-anchor-carousel-to") ?? "", 10);

          if (!Number.isNaN(targetIndex)) {
            moveTo(targetIndex);
          }
        });
      });

      carousel.addEventListener("mouseenter", clearAutoAdvance);
      carousel.addEventListener("mouseleave", restartAutoAdvance);
      carousel.addEventListener("focusin", clearAutoAdvance);
      carousel.addEventListener("focusout", () => {
        window.setTimeout(() => {
          if (!carousel.contains(document.activeElement)) {
            restartAutoAdvance();
          }
        }, 0);
      });
      carousel.addEventListener("keydown", (event) => {
        if (event.target.matches("input, textarea, select")) {
          return;
        }

        if (event.key === "ArrowLeft") {
          event.preventDefault();
          moveTo(activeIndex - 1);
        }

        if (event.key === "ArrowRight") {
          event.preventDefault();
          moveTo(activeIndex + 1);
        }
      });
      carousel.addEventListener("touchstart", (event) => {
        if (event.touches.length !== 1) {
          return;
        }

        touchStartX = event.touches[0].clientX;
        clearAutoAdvance();
      }, { passive: true });
      carousel.addEventListener("touchend", (event) => {
        if (touchStartX === null) {
          restartAutoAdvance();
          return;
        }

        const touchEndX = event.changedTouches.length > 0 ? event.changedTouches[0].clientX : touchStartX;
        const deltaX = touchEndX - touchStartX;
        touchStartX = null;

        if (Math.abs(deltaX) > 40) {
          moveTo(deltaX > 0 ? activeIndex - 1 : activeIndex + 1);
          return;
        }

        restartAutoAdvance();
      }, { passive: true });

      document.addEventListener("visibilitychange", () => {
        if (document.hidden) {
          clearAutoAdvance();
        } else {
          restartAutoAdvance();
        }
      });

      syncCarousel();
      restartAutoAdvance();
    });
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
      closeHeaderMenus(menuToggle);
      syncHeaderMenu(menuToggle, !isExpanded);
      syncHeaderHeight();
      return;
    }

    if (event.target.closest(headerMenuActionSelector)) {
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
      return;
    }

    if (event.key === "Escape") {
      closeHeaderMenus();
    }
  }

  function initialize() {
    applyTheme(resolveTheme());
    closeHeaderMenus();
    syncHeaderHeight();
    initializeCarousels();
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
