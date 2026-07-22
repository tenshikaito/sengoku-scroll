export abstract class StrategyApiSourceInfoBehavior {
  abstract readonly source: "live" | "mock" | "mock-fallback";

  abstract readonly message: string;
}

class LiveApiSourceInfoBehavior extends StrategyApiSourceInfoBehavior {
  readonly source = "live" as const;
  readonly message = "";
}

class MockApiSourceInfoBehavior extends StrategyApiSourceInfoBehavior {
  readonly source = "mock" as const;
  readonly message = "当前为 Mock 模式。";
}

class MockFallbackApiSourceInfoBehavior extends StrategyApiSourceInfoBehavior {
  readonly source = "mock-fallback" as const;
  readonly message = "Live API 不可达，已自动使用 Mock 数据（见下方诊断面板）。";
}

export function resolveStrategyApiSourceInfo(
  usingMockFallback: boolean,
  lastRequestSource: string | undefined | null,
): string {
  if (usingMockFallback) return new MockFallbackApiSourceInfoBehavior().message;
  if (lastRequestSource === "mock") return new MockApiSourceInfoBehavior().message;
  return new LiveApiSourceInfoBehavior().message;
}
