# 战国绘卷：原型美术规范与资产清单

## 使用边界

- 当前阶段素材只用于单机试玩原型，不宣称为最终商用资产。
- 所有生成素材必须保留生成提示、日期和来源；正式发布前逐项确认版权、人物肖像与字体许可。
- UI 信息、地图格和势力色仍由代码渲染，避免把文字烘焙进图片，也保证中英文与不同分辨率可用。

## 视觉方向

- 时代：日本战国中后期；建筑、甲胄和旗指物保持历史氛围，但不复刻具体影视或游戏作品。
- 风格：和纸、水墨、矿物颜料质感；地图与 UI 以深靛、赭石、旧金和灰白为主。
- 可读性：背景低对比、低细节，交互元素保持高对比；红绿不能作为唯一状态编码。
- 禁止：现代物件、现代城市轮廓、可读文字、水印、现成品牌标识、具体在世人物肖像。

## 原型资产清单

| 优先级 | 资产 | 建议规格 | 状态 |
|---|---|---:|---|
| P0 | 首页远景背景 | 2048×1152 WebP/PNG，无文字 | 已生成首版 |
| P0 | 势力纹章占位 | 256×256 SVG/PNG，透明底 | 继续使用代码图形，待设计确认 |
| P0 | 通用人物剪影 | 512×512 PNG，透明底 | 待生成 |
| P1 | 主要人物头像 | 768×768 PNG，统一视角与光线 | 待角色表确认后生成 |
| P1 | 事件插图 | 1536×1024 WebP，无文字 | 待事件清单确认后生成 |
| P1 | 城池/町/寺社图标 | 256×256 PNG，透明底 | 待地图符号规范确认 |
| P2 | 地形纹理组 | 512×512 无缝纹理 | Pixi 性能与缩放验证后制作 |
| P2 | 战斗与外交氛围图 | 1536×1024 WebP | 待正式事件流程 |

## 文件与命名

- 项目素材目录：`SengokuScroll.WebClient/public/assets/prototype/`。
- 文件名使用小写英文与连字符，例如 `landing-sengoku-landscape-v1.png`。
- 同一素材迭代使用 `-v2`、`-v3`，不覆盖已进入版本控制的旧稿。
- 头像预留路径：`portraits/<character-id>-v1.png`；事件图：`events/<event-key>-v1.webp`。

## 性能预算

- 首页背景压缩后目标小于 600 KB；头像单张小于 250 KB；事件图单张小于 500 KB。
- 首屏只预加载首页背景，人物头像与事件图按需懒加载。
- 不把数十张高清原图打进 JavaScript bundle；静态资源使用独立 URL 并允许浏览器缓存。

## 首版背景提示词

Use case: historical-scene. Asset type: desktop game landing-page background. A broad late-Sengoku Japanese landscape seen from a hill, distant mountain castle, winding river, rice fields and a few small marching banners, atmospheric depth, hand-painted sumi ink and muted mineral-pigment texture on aged washi paper, dark indigo and warm ochre palette, calm strategic mood, wide composition with a darker uncluttered center-left area for HTML title and buttons. No readable text, no logos, no watermark, no close-up identifiable historical person, no modern objects, no UI elements.
