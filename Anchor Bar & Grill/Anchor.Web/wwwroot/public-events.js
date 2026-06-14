(function () {
  const feedRootSelector = "[data-public-events-feed-root='true']";

  function initializePublicEventFeeds() {
    document.querySelectorAll(feedRootSelector).forEach(initializePublicEventFeed);
  }

  function initializePublicEventFeed(root) {
    if (!(root instanceof HTMLElement) || root.dataset.publicEventsInitialized === "true") {
      return;
    }

    const grid = root.querySelector("[data-public-events-grid='true']");
    const template = root.querySelector("[data-public-events-template='card']");
    const status = root.querySelector("[data-public-events-status='true']");
    const loadButton = root.querySelector("[data-public-events-load='true']");
    const sentinel = root.querySelector("[data-public-events-sentinel='true']");
    const feedUrl = root.getAttribute("data-public-events-feed-url");

    if (!(grid instanceof HTMLElement)
      || !(template instanceof HTMLTemplateElement)
      || !(status instanceof HTMLElement)
      || !(loadButton instanceof HTMLButtonElement)
      || !(sentinel instanceof HTMLElement)
      || !feedUrl) {
      return;
    }

    root.dataset.publicEventsInitialized = "true";

    let nextFromDate = root.getAttribute("data-public-events-next-from") ?? "";
    let hasMore = root.getAttribute("data-public-events-has-more") === "true";
    let isLoading = false;
    let observer = null;

    function setStatus(message) {
      status.textContent = message;
    }

    function syncFeedVisibility() {
      loadButton.hidden = !hasMore;

      if (!hasMore) {
        setStatus(
          grid.children.length > 0
            ? "You've reached the end of the published calendar."
            : "No published events are on the calendar right now.");
      }
    }

    function clearEmptyState() {
      root.querySelectorAll("[data-public-events-empty-state='true']").forEach((element) => {
        element.remove();
      });
    }

    function setRequiredText(card, selector, value) {
      const element = card.querySelector(selector);

      if (element instanceof HTMLElement) {
        element.textContent = typeof value === "string" ? value : "";
      }
    }

    function setOptionalText(card, selector, value) {
      const element = card.querySelector(selector);

      if (!(element instanceof HTMLElement)) {
        return;
      }

      const hasValue = typeof value === "string" && value.trim().length > 0;
      element.hidden = !hasValue;
      element.textContent = hasValue ? value : "";
    }

    function populateCard(card, item) {
      const hasImage = typeof item?.imagePath === "string" && item.imagePath.length > 0;
      const imageShell = card.querySelector("[data-public-event-image-shell='true']");
      const image = card.querySelector("[data-public-event-image='true']");
      const schedulePill = card.querySelector("[data-public-event-schedule-pill='true']");

      if (imageShell instanceof HTMLElement) {
        imageShell.hidden = !hasImage;
      }

      if (image instanceof HTMLImageElement) {
        if (hasImage) {
          image.src = item.imagePath;
          image.alt = typeof item.imageAltText === "string" ? item.imageAltText : "";
        } else {
          image.removeAttribute("src");
          image.alt = "";
        }
      }

      if (schedulePill instanceof HTMLElement) {
        schedulePill.textContent = typeof item?.scheduleTypeLabel === "string" ? item.scheduleTypeLabel : "";
        schedulePill.classList.toggle("status-pill--recurring", item?.isRecurring === true);
        schedulePill.classList.toggle("status-pill--one-time", item?.isRecurring !== true);
      }

      card.classList.toggle("event-card--with-image", hasImage);
      card.classList.toggle("event-card--text-only", !hasImage);

      setRequiredText(card, "[data-public-event-badge='true']", item?.promoBadge);
      setRequiredText(card, "[data-public-event-date='true']", item?.dateLabel);
      setRequiredText(card, "[data-public-event-title='true']", item?.title);
      setRequiredText(card, "[data-public-event-datetime='true']", item?.dateTimeLabel);
      setRequiredText(card, "[data-public-event-schedule='true']", item?.scheduleSummary);
      setOptionalText(card, "[data-public-event-summary='true']", item?.summary);
      setOptionalText(card, "[data-public-event-description='true']", item?.description);
    }

    function appendItems(items) {
      if (!Array.isArray(items) || items.length === 0) {
        return 0;
      }

      clearEmptyState();

      const fragment = document.createDocumentFragment();
      let appendedCount = 0;

      items.forEach((item) => {
        const card = template.content.firstElementChild?.cloneNode(true);

        if (!(card instanceof HTMLElement)) {
          return;
        }

        populateCard(card, item);
        fragment.appendChild(card);
        appendedCount += 1;
      });

      if (appendedCount > 0) {
        grid.appendChild(fragment);
      }

      return appendedCount;
    }

    function isSentinelVisible() {
      const bounds = sentinel.getBoundingClientRect();
      return bounds.top <= window.innerHeight * 1.1;
    }

    async function loadNextWindow() {
      if (isLoading || !hasMore || nextFromDate.length === 0) {
        return;
      }

      isLoading = true;
      loadButton.disabled = true;
      setStatus("Loading more upcoming dates...");

      let shouldContinue = false;

      try {
        const response = await fetch(`${feedUrl}?from=${encodeURIComponent(nextFromDate)}`, {
          headers: {
            Accept: "application/json"
          }
        });

        if (!response.ok) {
          throw new Error("The public events feed request failed.");
        }

        const data = await response.json();
        const appendedCount = appendItems(Array.isArray(data?.events) ? data.events : []);

        nextFromDate = typeof data?.nextFromDate === "string" ? data.nextFromDate : "";
        hasMore = data?.hasMore === true;

        root.setAttribute("data-public-events-next-from", nextFromDate);
        root.setAttribute("data-public-events-has-more", hasMore ? "true" : "false");

        if (hasMore) {
          setStatus("More published dates load automatically as you reach the end of the list.");
        } else {
          setStatus(
            grid.children.length > 0
              ? "You've reached the end of the published calendar."
              : "No published events are on the calendar right now.");
        }

        shouldContinue = hasMore && (appendedCount === 0 || isSentinelVisible());
      } catch {
        setStatus("We couldn't load more dates just now. Use the button to try again.");
      } finally {
        isLoading = false;
        loadButton.disabled = false;
        syncFeedVisibility();

        if (!hasMore && observer) {
          observer.disconnect();
        }

        if (shouldContinue) {
          window.setTimeout(() => {
            void loadNextWindow();
          }, 0);
        }
      }
    }

    loadButton.addEventListener("click", () => {
      void loadNextWindow();
    });

    syncFeedVisibility();

    if (!hasMore) {
      return;
    }

    if ("IntersectionObserver" in window) {
      observer = new IntersectionObserver((entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          void loadNextWindow();
        }
      }, { rootMargin: "240px 0px" });

      observer.observe(sentinel);

      if (isSentinelVisible()) {
        void loadNextWindow();
      }

      return;
    }

    if (isSentinelVisible()) {
      void loadNextWindow();
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializePublicEventFeeds, { once: true });
  } else {
    initializePublicEventFeeds();
  }
})();
