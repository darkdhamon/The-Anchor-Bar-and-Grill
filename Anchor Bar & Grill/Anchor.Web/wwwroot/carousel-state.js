(function (root, factory) {
  const api = factory();

  if (typeof module === "object" && module.exports) {
    module.exports = api;
  }

  root.anchorCarouselState = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function normalizeIndex(index, slideCount) {
    if (!Number.isInteger(slideCount) || slideCount < 1) {
      return 0;
    }

    return ((index % slideCount) + slideCount) % slideCount;
  }

  function getSlideState(index, activeIndex, slideCount) {
    if (index === activeIndex) {
      return "is-active";
    }

    if (slideCount > 1 && index === normalizeIndex(activeIndex - 1, slideCount)) {
      return "is-prev-1";
    }

    if (slideCount > 2 && index === normalizeIndex(activeIndex - 2, slideCount)) {
      return "is-prev-2";
    }

    if (slideCount > 1 && index === normalizeIndex(activeIndex + 1, slideCount)) {
      return "is-next-1";
    }

    if (slideCount > 2 && index === normalizeIndex(activeIndex + 2, slideCount)) {
      return "is-next-2";
    }

    return "is-hidden";
  }

  function moveTo(nextIndex, slideCount) {
    return normalizeIndex(nextIndex, slideCount);
  }

  return { getSlideState, moveTo };
});
