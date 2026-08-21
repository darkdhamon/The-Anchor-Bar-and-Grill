const assert = require("node:assert/strict");
const path = require("node:path");
const state = require(path.join(__dirname, "..", "..", "Anchor.Web", "wwwroot", "carousel-state.js"));

assert.equal(state.moveTo(1, 5), 1, "moves forward to the requested slide");
assert.equal(state.moveTo(5, 5), 0, "wraps forward from the final slide");
assert.equal(state.moveTo(-1, 5), 4, "wraps backward from the first slide");
assert.equal(state.moveTo(3, 0), 0, "returns a safe index when there are no slides");

const expectedStates = ["is-active", "is-next-1", "is-next-2", "is-prev-2", "is-prev-1"];
assert.deepEqual(
  expectedStates.map((_, index) => state.getSlideState(index, 0, 5)),
  expectedStates,
  "assigns active, adjacent, and second-neighbor states"
);

assert.equal(state.getSlideState(0, 1, 2), "is-next-1", "handles the two-slide boundary without duplicate neighbor states");
assert.deepEqual(
  [0, 1, 2].map((index) => state.getSlideState(index, 0, 3)),
  ["is-active", "is-next-1", "is-prev-1"],
  "keeps both sides populated for three slides"
);
assert.deepEqual(
  [0, 1, 2, 3].map((index) => state.getSlideState(index, 0, 4)),
  ["is-active", "is-next-1", "is-next-2", "is-prev-1"],
  "assigns the equidistant fourth slide once without overlapping states"
);
assert.equal(state.getSlideState(4, 2, 6), "is-next-2", "assigns the second-next state");
assert.equal(state.getSlideState(5, 2, 6), "is-hidden", "hides slides outside the visible five-slide window");

assert.equal(state.shouldAutoAdvance(5, false, false), true, "auto-advances an unfocused visible carousel");
assert.equal(state.shouldAutoAdvance(1, false, false), false, "does not auto-advance a single slide");
assert.equal(state.shouldAutoAdvance(5, true, false), false, "pauses while the document is hidden");
assert.equal(state.shouldAutoAdvance(5, false, true), false, "stays paused while focus remains inside the carousel");

console.log("Carousel state tests passed.");
