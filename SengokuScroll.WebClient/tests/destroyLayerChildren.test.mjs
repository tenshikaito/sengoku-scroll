import assert from "node:assert/strict";
import { test } from "node:test";
import { readFileSync } from "node:fs";
import ts from "typescript";

const source = readFileSync(new URL("../src/utils/destroyLayerChildren.ts", import.meta.url), "utf8");
const js = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const { destroyLayerChildren } = await import(`data:text/javascript;base64,${Buffer.from(js).toString("base64")}`);

test("redraw destroys every detached child and its descendants once", () => {
  let destroyed = 0;
  let children = Array.from({ length: 100 }, () => ({ destroy(options) {
    assert.deepEqual(options, { children: true }); destroyed++;
  } }));
  const layer = { removeChildren() { const old = children; children = []; return old; } };
  destroyLayerChildren(layer);
  destroyLayerChildren(layer);
  assert.equal(destroyed, 100);
  assert.equal(children.length, 0);
});

test("all map layers use resource cleanup and asynchronous initialization guards unmount", () => {
  const component = readFileSync(new URL("../src/components/strategy/StrategyMapCanvas.vue", import.meta.url), "utf8");
  for (const name of ["mapLayer", "entityLayer", "pathLayer", "highlightLayer"])
    assert.ok(component.includes(`destroyLayerChildren(${name})`));
  assert.ok(component.includes("generation !== pixiGeneration"));
  assert.ok(component.includes("initializingApp.destroy(true, { children: true })"));
});
