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

assert.equal(state.getSlideState(0, 1, 2), "is-prev-1", "handles the two-slide boundary without duplicate second-neighbor states");
assert.equal(state.getSlideState(4, 2, 6), "is-next-2", "assigns the second-next state");
assert.equal(state.getSlideState(5, 2, 6), "is-hidden", "hides slides outside the visible five-slide window");

console.log("Carousel state tests passed.");
