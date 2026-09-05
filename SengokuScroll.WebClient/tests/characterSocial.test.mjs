import assert from "node:assert/strict";
import { test } from "node:test";
import { readFileSync } from "node:fs";
import ts from "typescript";
const source = readFileSync(new URL("../src/utils/characterSocialError.ts", import.meta.url), "utf8");
const javascript = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const { characterSocialError } = await import(`data:text/javascript;base64,${Buffer.from(javascript).toString("base64")}`);
test("social errors explain cooldown and marriage eligibility", () => {
  assert.match(characterSocialError("400: SocialCooldown"), /7天/);
  assert.match(characterSocialError("MarriageIneligible"), /18岁/);
  assert.equal(characterSocialError("disconnected"), "disconnected");
});
test("character panel exposes proposal consent decline and actual memories", () => {
  const component = readFileSync(new URL("../src/components/strategy/intel/StrategyIntelPersonPane.vue", import.meta.url), "utf8");
  for (const text of ["'Marry'", "'DeclineMarriage'", "pendingMarriageFromId", "socialMemories", "defectionWarningDay"])
    assert.ok(component.includes(text));
});
