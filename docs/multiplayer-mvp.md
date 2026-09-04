# 1–8 人大战略联机 MVP

## 当前能力

- 内存大厅：创建、列出、加入和退出房间。
- 每个房间独立拥有一个 `StrategySimulationHost`、世界、随机种子和 DI 作用域。
- 势力独占：同一势力同时只能由一名玩家占用。
- 请求身份：房间号与 192-bit 随机玩家令牌。
- 服务器权威：客户端沿用现有战略 API，但服务端在房间锁内切换观察/行动势力并执行全部规则校验。
- 命令去重：联机写请求要求唯一 `X-Sengoku-Command-Id`，最近保留 2048 个命令号。
- 统一时钟：所有在线玩家准备后只推进一天，随后清空准备状态。
- 断线容错：客户端轮询会续订在线状态；SignalR 断连或约 12 秒无活动后不再阻塞准备，重连时恢复原凭据和势力。
- AI 托管：无人占用的势力由 AI 控制；真人势力的当主不被人物 AI 接管。
- 独立情报：世界状态在映射前切换到请求者势力，迷雾、外交和谍报不会跨玩家泄漏。
- 同步：服务端提供 SignalR 房间组和 `WorldChanged` 通知；当前内置网页客户端同时使用 2.5 秒轮询作为可靠回退。
- 重新加入：浏览器保存房间凭据，刷新页面后调用 reconnect 恢复会话。

## 请求约定

进入房间后的既有 `/api/strategy/*` 请求需要：

```http
X-Sengoku-Room-Id: 10位房间代码
X-Sengoku-Player-Token: 玩家私有令牌
X-Sengoku-Command-Id: 每个写命令唯一 UUID
```

响应包含当前 `X-Sengoku-World-Version`。直接加载剧本、直接推进日期、即时战斗、单机存档导出/恢复和存档槽接口在房间上下文中返回 403，防止客户端绕开房间规则。

大厅 API：

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/multiplayer/rooms` | 房间列表 |
| POST | `/api/multiplayer/rooms` | 创建房间并返回房主凭据 |
| GET | `/api/multiplayer/rooms/{roomId}` | 房间与玩家准备状态 |
| POST | `/api/multiplayer/rooms/{roomId}/join` | 占用未被选择的势力 |
| POST | `/api/multiplayer/rooms/{roomId}/reconnect` | 使用原玩家 ID 和令牌重连 |
| POST | `/api/multiplayer/rooms/{roomId}/ready` | 设置准备；全员准备时推进一天 |
| POST | `/api/multiplayer/rooms/{roomId}/leave` | 主动离开并释放势力 |
| GET | `/api/multiplayer/scenarios/{scenarioId}/forces` | 创建房间前列出可玩势力 |

SignalR Hub：`/hubs/strategy`。客户端连接后调用 `JoinRoom(roomId, playerToken)`，服务端使用 `RoomJoined` 和 `WorldChanged` 事件通知房间变化。

## 并发与多核边界

- 不同房间可以由 ASP.NET Core 线程池并行处理，天然利用多核。
- 单个房间使用 `SemaphoreSlim` 串行化命令、准备推进和玩家状态修改。
- 房间内部继续使用 `StrategyParallelWork` 处理只读视野、AI 感知、DTO 和地图投影。
- 世界写操作仍固定顺序提交，保持同种子回放确定性。

不要把一个房间内的两个玩家命令并行写进 `GameWorld`。扩展到多进程时，应按 `RoomId` 将整个房间固定路由到一个 actor/节点。

## 局域网运行

开发环境：

```powershell
dotnet run --project .\SengokuScroll.WebApi\SengokuScroll.WebApi.csproj --launch-profile lan
```

发行版默认监听 `http://0.0.0.0:5100`。主机自己访问 `http://127.0.0.1:5100/`，其他设备访问 `http://主机局域网IP:5100/`。只应在可信局域网和 Windows“专用网络”防火墙范围内开放端口。

## 当前限制

- 房间、玩家令牌和世界只保存在主机内存，进程重启后失效。
- 令牌是访客房间凭据，不是正式账号系统；没有密码、JWT、封禁或角色所有权数据库。
- 当前没有 TLS、公开互联网匹配、NAT 穿透、邀请链接或反作弊遥测。
- SignalR 服务端通知已经提供，内置客户端以轮询为可靠基线，尚未引入 SignalR JavaScript 客户端包。
- 战报/经济消息仍沿用单机“当前玩家势力”生成路径；世界结果一致，但后续需要把私人事件改为按势力分别投递。
- 房间没有持久化快照和命令日志恢复；联机存档槽被主动禁用。
- MMO 的账号、聊天、长期世界、分区、国战实例和数据库不在此 MVP 范围内。

## 后续生产化顺序

1. PostgreSQL/SQLite 房间快照、追加式命令日志与崩溃恢复。
2. ASP.NET Core Identity/OIDC、账号角色和邀请权限。
3. 前端 SignalR 客户端、版本差量和丢包后的完整快照回退。
4. 按势力分发战报、经济报告、通知和聊天频道。
5. 房主转移、踢人、准备超时、暂停投票和可配置的 AI 接管计时。
6. 反重放持久化、速率限制、TLS、审计日志和互联网安全测试。
7. Redis 房间路由与多节点部署；一个房间始终只由一个权威节点写入。
