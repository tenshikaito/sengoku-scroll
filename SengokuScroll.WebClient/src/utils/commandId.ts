/** getRandomValues also works on LAN HTTP; randomUUID requires a secure context. */
export function createCommandId(): string {
  const bytes = new Uint8Array(16);
  globalThis.crypto.getRandomValues(bytes);
  return Array.from(bytes, value => value.toString(16).padStart(2, "0")).join("");
}
