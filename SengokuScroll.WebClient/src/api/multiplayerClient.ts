import type { StrategyAdvanceDayResponse } from "./strategyTypes";
import { normalizeStrategyWorldState } from "@/utils/normalizeStrategyWorldState";
import { normalizeBattleResult } from "@/utils/battleResult";
import { normalizeStrategyEvent } from "@/utils/normalizeStrategyEvent";
import { createCommandId } from "@/utils/commandId";

const SESSION_KEY = "sengoku_scroll_multiplayer_session_v1";

export interface MultiplayerSession {
  roomId: string;
  roomName: string;
  playerId: string;
  playerToken: string;
  playerName: string;
  forceId: number;
  isHost: boolean;
}

export interface MultiplayerPlayer {
  playerId: string;
  playerName: string;
  forceId: number;
  isHost: boolean;
  ready: boolean;
  connected: boolean;
}

export interface MultiplayerForce {
  forceId: number;
  forceName: string;
  category: string;
  occupied: boolean;
}

export interface MultiplayerRoom {
  roomId: string;
  roomName: string;
  scenarioId: string;
  status: "Waiting" | "Running" | string;
  maxPlayers: number;
  playerCount: number;
  worldVersion: number;
  turnNumber: number;
  players: MultiplayerPlayer[];
  forces: MultiplayerForce[];
}

interface RoomCredentials {
  playerId: string;
  playerToken: string;
  forceId: number;
  isHost: boolean;
}

interface RoomJoinResponse {
  room: MultiplayerRoom;
  credentials: RoomCredentials;
}

export class MultiplayerRequestError extends Error {
  constructor(public readonly status: number, public readonly code: string) {
    super(`联机请求失败：${code}`);
  }
}

export interface MultiplayerReadyResult {
  room: MultiplayerRoom;
  advance: StrategyAdvanceDayResponse;
  advanced: boolean;
}

export function readMultiplayerSession(): MultiplayerSession | null {
  try {
    // Migrate an existing session once; separate tabs can then play distinct forces.
    const legacy = localStorage.getItem(SESSION_KEY);
    if (!sessionStorage.getItem(SESSION_KEY) && legacy) {
      sessionStorage.setItem(SESSION_KEY, legacy);
      localStorage.removeItem(SESSION_KEY);
    }
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const value = JSON.parse(raw) as Partial<MultiplayerSession>;
    if (!value.roomId || !value.playerId || !value.playerToken || !value.forceId) return null;
    return value as MultiplayerSession;
  } catch {
    return null;
  }
}

export function isMultiplayerSessionActive(): boolean {
  return readMultiplayerSession() !== null;
}

export function clearMultiplayerSession(): void {
  sessionStorage.removeItem(SESSION_KEY);
  localStorage.removeItem(SESSION_KEY);
}

function saveSession(response: RoomJoinResponse, playerName: string): MultiplayerSession {
  const session: MultiplayerSession = {
    roomId: response.room.roomId,
    roomName: response.room.roomName,
    playerId: response.credentials.playerId,
    playerToken: response.credentials.playerToken,
    playerName,
    forceId: response.credentials.forceId,
    isHost: response.credentials.isHost,
  };
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
  return session;
}

async function multiplayerFetch<T>(
  method: string,
  path: string,
  body?: unknown,
  session: MultiplayerSession | null = readMultiplayerSession(),
): Promise<T> {
  const headers: HeadersInit = { "Content-Type": "application/json" };
  if (session) {
    headers["X-Sengoku-Room-Id"] = session.roomId;
    headers["X-Sengoku-Player-Token"] = session.playerToken;
    if (method !== "GET" && method !== "HEAD") {
      headers["X-Sengoku-Command-Id"] = createCommandId();
    }
  }

  const response = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    let code = response.statusText;
    try {
      const error = (await response.json()) as { errorCode?: string };
      code = error.errorCode ?? code;
    } catch {
      // Keep the HTTP status text when the server did not return JSON.
    }
    throw new MultiplayerRequestError(response.status, code);
  }
  return (await response.json()) as T;
}

export const listMultiplayerRooms = () =>
  multiplayerFetch<MultiplayerRoom[]>("GET", "/api/multiplayer/rooms", undefined, null);

export function heartbeatMultiplayerRoom(session: MultiplayerSession): Promise<MultiplayerRoom> {
  return multiplayerFetch<MultiplayerRoom>("GET",
    `/api/multiplayer/rooms/${encodeURIComponent(session.roomId)}/heartbeat`, undefined, session);
}

export const listMultiplayerScenarioForces = (scenarioId = "mini_kanto") =>
  multiplayerFetch<MultiplayerForce[]>(
    "GET",
    `/api/multiplayer/scenarios/${encodeURIComponent(scenarioId)}/forces`,
    undefined,
    null,
  );

export async function createMultiplayerRoom(input: {
  roomName: string;
  playerName: string;
  forceId: number;
  maxPlayers: number;
}): Promise<{ room: MultiplayerRoom; session: MultiplayerSession }> {
  const response = await multiplayerFetch<RoomJoinResponse>(
    "POST",
    "/api/multiplayer/rooms",
    {
      ...input,
      scenarioId: "mini_kanto",
      difficulty: "Normal",
    },
    null,
  );
  return { room: response.room, session: saveSession(response, input.playerName) };
}

export async function joinMultiplayerRoom(input: {
  roomId: string;
  playerName: string;
  forceId: number;
}): Promise<{ room: MultiplayerRoom; session: MultiplayerSession }> {
  const roomId = input.roomId.trim().toUpperCase();
  const response = await multiplayerFetch<RoomJoinResponse>(
    "POST",
    `/api/multiplayer/rooms/${encodeURIComponent(roomId)}/join`,
    { playerName: input.playerName, forceId: input.forceId },
    null,
  );
  return { room: response.room, session: saveSession(response, input.playerName) };
}

export async function reconnectMultiplayerSession(): Promise<MultiplayerRoom | null> {
  const session = readMultiplayerSession();
  if (!session) return null;
  const response = await multiplayerFetch<RoomJoinResponse>(
    "POST",
    `/api/multiplayer/rooms/${encodeURIComponent(session.roomId)}/reconnect`,
    { playerId: session.playerId, playerToken: session.playerToken },
    session,
  );
  saveSession(response, session.playerName);
  return response.room;
}

export async function leaveMultiplayerRoom(): Promise<void> {
  const session = readMultiplayerSession();
  if (!session) return;
  try {
    await multiplayerFetch(
      "POST",
      `/api/multiplayer/rooms/${encodeURIComponent(session.roomId)}/leave`,
      {},
      session,
    );
  } catch (error) {
    if (!(error instanceof MultiplayerRequestError) || ![401, 404].includes(error.status)) throw error;
  }
  clearMultiplayerSession();
}

export async function setMultiplayerReady(ready = true): Promise<MultiplayerReadyResult> {
  const session = readMultiplayerSession();
  if (!session) throw new Error("没有有效的联机房间会话");
  const room = await multiplayerFetch<MultiplayerRoom>(
    "GET", `/api/multiplayer/rooms/${encodeURIComponent(session.roomId)}`, undefined, session,
  );
  const response = await multiplayerFetch<{
    room: MultiplayerRoom;
    advance: Record<string, unknown>;
    advanced: boolean;
  }>(
    "POST",
    `/api/multiplayer/rooms/${encodeURIComponent(session.roomId)}/ready`,
    { ready, expectedTurn: room.turnNumber },
    session,
  );
  const rawAdvance = response.advance;
  const battles = rawAdvance.resolvedBattles ?? rawAdvance.ResolvedBattles;
  const events = rawAdvance.events ?? rawAdvance.Events;
  return {
    room: response.room,
    advanced: response.advanced,
    advance: {
      state: normalizeStrategyWorldState(rawAdvance.state ?? rawAdvance.State),
      resolvedBattles: Array.isArray(battles) ? battles.map(normalizeBattleResult) : [],
      events: Array.isArray(events) ? events.map(normalizeStrategyEvent) : [],
      daysAdvanced: Number(rawAdvance.daysAdvanced ?? rawAdvance.DaysAdvanced ?? 0),
    },
  };
}
