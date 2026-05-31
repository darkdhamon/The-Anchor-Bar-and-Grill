export function scrollRelevantOption(menuElement, selector) {
    if (!menuElement || !selector) {
        return;
    }

    const target = menuElement.querySelector(selector);
    if (!target) {
        return;
    }

    const centeredTop = target.offsetTop - ((menuElement.clientHeight - target.clientHeight) / 2);
    menuElement.scrollTop = Math.max(0, centeredTop);
}
