import assert from "node:assert/strict";
import { test } from "node:test";
import { readFileSync } from "node:fs";
import ts from "typescript";

const source = readFileSync(new URL("../src/utils/requestTimeout.ts", import.meta.url), "utf8");
const javascript = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
}).outputText;
const { withRequestTimeout } =
  await import(`data:text/javascript;base64,${Buffer.from(javascript).toString("base64")}`);

test("successful request returns its value without aborting", async () => {
  let signal;
  assert.equal(await withRequestTimeout(async s => { signal = s; return 42; }, 20), 42);
  await new Promise(resolve => setTimeout(resolve, 40));
  assert.equal(signal.aborted, false);
});

test("stalled request times out, aborts and is not retried", async () => {
  let signal;
  let calls = 0;
  await assert.rejects(withRequestTimeout(s => {
    signal = s;
    calls++;
    return new Promise(() => {});
  }, 20), { name: "RequestTimeoutError" });
  assert.equal(signal.aborted, true);
  assert.equal(calls, 1);
  assert.equal(await withRequestTimeout(async () => "next poll", 20), "next poll");
});

test("deadline also covers decoding a stalled response body", async () => {
  await assert.rejects(withRequestTimeout(async () => {
    const response = { json: () => new Promise(() => {}) };
    return await response.json();
  }, 20), { name: "RequestTimeoutError" });
});

test("original errors are preserved and deadline is cleaned up", async () => {
  const failure = new Error("server rejected command");
  let signal;
  await assert.rejects(withRequestTimeout(s => {
    signal = s;
    throw failure;
  }, 20), error => error === failure);
  await new Promise(resolve => setTimeout(resolve, 40));
  assert.equal(signal.aborted, false);
});
