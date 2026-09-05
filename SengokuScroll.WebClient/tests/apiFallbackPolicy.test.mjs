import assert from "node:assert/strict";
import { test } from "node:test";
import { readFileSync } from "node:fs";
import ts from "typescript";

const source = readFileSync(new URL("../src/api/apiFallbackPolicy.ts", import.meta.url), "utf8");
const javascript = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
}).outputText;
const { canUseInitialMockFallback, LiveRequestError } =
  await import(`data:text/javascript;base64,${Buffer.from(javascript).toString("base64")}`);

test("rejected or disconnected write commands never switch to mock", () => {
  for (const status of [null, 400, 403, 409, 500, 502]) {
    assert.equal(canUseInitialMockFallback("POST", "/load", new LiveRequestError("error", status), false), false);
    assert.equal(canUseInitialMockFallback("POST", "/units/1/move", new LiveRequestError("error", status), false), false);
  }
});

test("live session disconnect preserves the real game", () => {
  assert.equal(canUseInitialMockFallback("GET", "/state", new LiveRequestError("offline", null), true), false);
});

test("only an initial unavailable read may use the demo", () => {
  assert.equal(canUseInitialMockFallback("GET", "/state", new LiveRequestError("offline", null), false), true);
  assert.equal(canUseInitialMockFallback("GET", "/map", new LiveRequestError("gateway", 502), false), true);
  assert.equal(canUseInitialMockFallback("GET", "/state", new LiveRequestError("forbidden", 403), false), false);
  assert.equal(canUseInitialMockFallback("GET", "/state", new Error("mapping bug"), false), false);
});
