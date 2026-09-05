export class LiveRequestError extends Error {
  constructor(message: string, public readonly status: number | null) {
    super(message);
  }
}

/** A failed real command must never become a successful command in another world. */
export function canUseInitialMockFallback(
  method: string, path: string, error: unknown, hasLiveSession: boolean,
): boolean {
  return !hasLiveSession && method === "GET" && ["/state", "/map"].includes(path)
    && error instanceof LiveRequestError
    && (error.status === null || [502, 503, 504].includes(error.status));
}
