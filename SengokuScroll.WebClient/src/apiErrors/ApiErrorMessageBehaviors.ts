import { parseApiErrorCode } from "@/utils/strategyActionRules";

export interface ApiErrorResolveContext {
  fallbackMessage: string;
  lordNotAtResidenceMessage?: string;
  lordAtResidenceTip?: string;
  lordCommandStrongholdTip?: string;
  characterGateApCost?: number;
  attackApBlockReason?: string | null;
  dataNotFoundHint?: string;
  dataNotFoundReloadHint?: string;
}

export type ApiErrorResolution =
  | { type: "message"; message: string }
  | { type: "blocked"; title: string; message: string }
  | { type: "none" };

export abstract class ApiErrorMessageBehavior {
  abstract readonly code: string;

  abstract resolve(message: string, ctx: ApiErrorResolveContext): ApiErrorResolution;
}

class DataNotFoundErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "DataNotFound";

  resolve(_message: string, ctx: ApiErrorResolveContext): ApiErrorResolution {
    return {
      type: "message",
      message: ctx.dataNotFoundReloadHint ?? ctx.dataNotFoundHint ?? ctx.fallbackMessage,
    };
  }
}

class LordNotAtResidenceErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "LordNotAtResidence";

  resolve(_message: string, ctx: ApiErrorResolveContext): ApiErrorResolution {
    if (ctx.lordNotAtResidenceMessage) {
      return { type: "message", message: ctx.lordNotAtResidenceMessage };
    }
    return {
      type: "message",
      message: ctx.lordCommandStrongholdTip ?? ctx.lordAtResidenceTip ?? ctx.fallbackMessage,
    };
  }
}

class NotSelfForceErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "NotSelfForce";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "仅可调整本家非内藩据点税率" };
  }
}

class CannotAppointLordToResidenceErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CannotAppointLordToResidence";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "当主居城须保持直辖" };
  }
}

class CharacterNotAtResidenceErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterNotAtResidence";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "将领须在当主居城方可任命" };
  }
}

class CharacterIsStrongholdLordErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterIsStrongholdLord";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "该将领已担任据点领主，不能兼任代官" };
  }
}

class CharacterIsForceLordErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterIsForceLord";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "当主不能担任据点代官" };
  }
}

class CharacterIsStrongholdMayorErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterIsStrongholdMayor";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "该将领已担任据点代官，无法调动" };
  }
}

class CharacterAlreadyAtStrongholdErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterAlreadyAtStronghold";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "该将领已在目标据点" };
  }
}

class CharacterHasActiveTaskErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterHasActiveTask";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "该将领已有任务，无法调动" };
  }
}

class CharacterNotOnRecallableTaskErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "CharacterNotOnRecallableTask";

  resolve(): ApiErrorResolution {
    return { type: "message", message: "该将领当前没有可召回的外派任务" };
  }
}

class ApNotEnoughErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "ApNotEnough";

  resolve(_message: string, ctx: ApiErrorResolveContext): ApiErrorResolution {
    if (ctx.attackApBlockReason) {
      return {
        type: "blocked",
        title: "无法攻击",
        message: ctx.attackApBlockReason,
      };
    }

    if (ctx.characterGateApCost != null) {
      return {
        type: "message",
        message: `行动力不足（出入城需 ${ctx.characterGateApCost} AP）`,
      };
    }

    return { type: "none" };
  }
}

class StrongholdBlockadedErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "StrongholdBlockaded";

  resolve(_message: string, ctx: ApiErrorResolveContext): ApiErrorResolution {
    const action = ctx.fallbackMessage.includes("入城") ? "入城" : "出城";
    return {
      type: "message",
      message: `据点被围，需确认强行${action}`,
    };
  }
}

class InsufficientGarrisonTroopsErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "InsufficientGarrisonTroops";

  resolve(): ApiErrorResolution {
    return {
      type: "message",
      message: "驻城兵种池兵力不足，无法按此编组出征",
    };
  }
}

class UnitNotDirectlyControllableErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "UnitNotDirectlyControllable";

  resolve(): ApiErrorResolution {
    return {
      type: "message",
      message: "指令模式：仅当主所在格部队可直接操作",
    };
  }
}

class MarketTradeNotFilledErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "TradeNotFilled";

  resolve(): ApiErrorResolution {
    return {
      type: "message",
      message:
        "未能成交：请检查限价是否覆盖对手盘、资金/粮食是否充足；商队仅支持即时砸单，限价须不低于卖一",
    };
  }
}

class MarketTradeNotAllowedErrorBehavior extends ApiErrorMessageBehavior {
  readonly code = "TradeNotAllowed";

  resolve(_message: string, ctx: ApiErrorResolveContext): ApiErrorResolution {
    return {
      type: "message",
      message: ctx.lordCommandStrongholdTip ?? "当前无法在此据点交易",
    };
  }
}

const API_ERROR_BEHAVIORS: ApiErrorMessageBehavior[] = [
  new DataNotFoundErrorBehavior(),
  new MarketTradeNotFilledErrorBehavior(),
  new MarketTradeNotAllowedErrorBehavior(),
  new LordNotAtResidenceErrorBehavior(),
  new NotSelfForceErrorBehavior(),
  new CannotAppointLordToResidenceErrorBehavior(),
  new CharacterNotAtResidenceErrorBehavior(),
  new CharacterIsStrongholdLordErrorBehavior(),
  new CharacterIsForceLordErrorBehavior(),
  new CharacterIsStrongholdMayorErrorBehavior(),
  new CharacterAlreadyAtStrongholdErrorBehavior(),
  new CharacterHasActiveTaskErrorBehavior(),
  new CharacterNotOnRecallableTaskErrorBehavior(),
  new ApNotEnoughErrorBehavior(),
  new StrongholdBlockadedErrorBehavior(),
  new InsufficientGarrisonTroopsErrorBehavior(),
  new UnitNotDirectlyControllableErrorBehavior(),
];

function resolveErrorCode(message: string): string | null {
  return parseApiErrorCode(message) ?? API_ERROR_BEHAVIORS.find((b) => message.includes(b.code))?.code ?? null;
}

export function resolveStrategyApiError(
  message: string,
  ctx: ApiErrorResolveContext,
): ApiErrorResolution {
  const code = resolveErrorCode(message);
  if (!code) return { type: "none" };

  const behavior = API_ERROR_BEHAVIORS.find((b) => b.code === code);
  if (!behavior) return { type: "none" };

  return behavior.resolve(message, ctx);
}

export async function applyStrategyApiErrorResolution(
  resolution: ApiErrorResolution,
  setError: (message: string) => void,
  notifyBlocked: (title: string, message: string) => Promise<void>,
  fallbackMessage: string,
): Promise<void> {
  switch (resolution.type) {
    case "message":
      setError(resolution.message);
      return;
    case "blocked":
      await notifyBlocked(resolution.title, resolution.message);
      return;
    case "none":
      setError(fallbackMessage);
      return;
  }
}
