import assert from "node:assert/strict";
import { test } from "node:test";
import { readFileSync } from "node:fs";

test("live view rows never fabricate historical effects when server views are empty", () => {
  const source = readFileSync(new URL("../src/utils/strategyIntelSystemData.ts", import.meta.url), "utf8");
  for (const name of ["PERSON_OUR_VIEW_EFFECTS", "PERSON_THEIR_VIEW_EFFECTS", "FORCE_OUR_VIEW_EFFECTS", "FORCE_THEIR_VIEW_EFFECTS"])
    assert.equal(source.includes(name), false);
  for (const name of ["personViewOfCharacterRows", "personCharacterViewOfLordRows", "forceOurViewEffectRows", "forceTheirViewEffectRows"]) {
    const body = source.slice(source.indexOf(`export function ${name}(`)).split("\n}")[0];
    assert.ok(body.includes("return [];"));
  }
});
