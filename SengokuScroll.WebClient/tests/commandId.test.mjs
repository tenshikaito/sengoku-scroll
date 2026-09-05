import assert from "node:assert/strict";
import { test } from "node:test";
import { readFileSync } from "node:fs";
import ts from "typescript";

const source = readFileSync(new URL("../src/utils/commandId.ts", import.meta.url), "utf8");
const javascript = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.ESNext },
}).outputText;
const { createCommandId } = await import(`data:text/javascript;base64,${Buffer.from(javascript).toString("base64")}`);

test("LAN HTTP crypto without randomUUID still generates command IDs", () => {
  const cryptoDescriptor = Object.getOwnPropertyDescriptor(globalThis, "crypto");
  Object.defineProperty(globalThis, "crypto", {
    configurable: true,
    value: { getRandomValues: bytes => bytes.fill(0xab) },
  });
  try { assert.equal(createCommandId(), "ab".repeat(16)); }
  finally { Object.defineProperty(globalThis, "crypto", cryptoDescriptor); }
});

test("command IDs use random bytes and fit the server length limit", () => {
  const ids = Array.from({ length: 1000 }, () => createCommandId());
  assert.equal(new Set(ids).size, ids.length);
  assert.ok(ids.every(id => /^[0-9a-f]{32}$/.test(id)));
});
