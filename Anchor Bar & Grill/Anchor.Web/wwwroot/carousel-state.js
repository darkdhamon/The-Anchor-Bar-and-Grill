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
    const offset = normalizeIndex(index - activeIndex, slideCount);

    if (offset === 0) {
      return "is-active";
    }

    if (slideCount > 1 && offset === 1) {
      return "is-next-1";
    }

    if (slideCount > 1 && offset === slideCount - 1) {
      return "is-prev-1";
    }

    if (slideCount > 3 && offset === 2) {
      return "is-next-2";
    }

    if (slideCount > 4 && offset === slideCount - 2) {
      return "is-prev-2";
    }

    return "is-hidden";
  }

  function moveTo(nextIndex, slideCount) {
    return normalizeIndex(nextIndex, slideCount);
  }

  function shouldAutoAdvance(slideCount, documentHidden, containsFocus) {
    return slideCount > 1 && !documentHidden && !containsFocus;
  }

  return { getSlideState, moveTo, shouldAutoAdvance };
});
