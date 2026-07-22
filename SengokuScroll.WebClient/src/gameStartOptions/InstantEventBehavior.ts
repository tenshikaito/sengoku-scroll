/** 即时事件摘要通道行为。 */
export abstract class InstantEventBehavior {
  abstract readonly enabled: boolean;
}

export class EnabledInstantEventBehavior extends InstantEventBehavior {
  readonly enabled = true;
}

export class DisabledInstantEventBehavior extends InstantEventBehavior {
  readonly enabled = false;
}

export class InstantEventBehaviorFactory {
  static create(
    difficulty: string | undefined,
    instantEventMessages: boolean,
  ): InstantEventBehavior {
    if (difficulty === "Easy") return new EnabledInstantEventBehavior();
    return instantEventMessages
      ? new EnabledInstantEventBehavior()
      : new DisabledInstantEventBehavior();
  }
}
