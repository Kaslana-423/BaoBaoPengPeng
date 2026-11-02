# 《团子碰碰物语》阶段一：三对三弹射原型

后续已增加一次反弹瞄准，以及角色攻击力、实际血量、碰撞伤害与底部 UI。当前场景位于 `Assets/Scenes/SC_Battle.unity`；新增属性与验收说明见 `README_角色属性.md`。以下保留阶段一的原始交付记录。

实际项目版本：**Unity 2022.3.50f1c1**。当前项目使用旧版 Input Manager，脚本与此设置匹配，同时保留 Unity 6 刚体 API 条件适配。

本次范围按任务书第 15 节执行：只完成角色选择、拖拽蓄力、瞄准、物理弹射、碰撞和阻尼。场景为我方三名、敌方三名。敌方本阶段是可被撞动的物理目标。

## 本阶段新增脚本

所有代码使用 `TuanziArena` 命名空间。

| 文件（相对 Assets/_Game） | 职责与关联 |
| --- | --- |
| `Scripts/Units/UnitActor.cs` | 角色阵营、刚体、圆形碰撞体、外观和选中效果；读取 UnitData，提供瞬时冲量入口。 |
| `Scripts/Input/PlayerInputController.cs` | 左键选择己方、计算反向拖拽向量；通过 PrototypeSession 请求弹射，并更新 AimPreview。 |
| `Scripts/Input/AimPreview.cs` | 绘制有总长度上限的虚线，最多显示一次反弹；取消和发射时隐藏。详见 README_瞄准辅助.md。 |
| `Scripts/Core/PrototypeSession.cs` | 本阶段的物理测试输入门禁；运动时禁止重复弹射，六个角色停稳后恢复选择。此脚本不实现回合或战斗状态机。 |
| `Scripts/Data/UnitData.cs` | ScriptableObject 静态物理配置：质量、阻尼、弹射倍率、物理材质、图片和阵营颜色。本阶段不包含生命值与伤害配置。 |
| `Editor/PrototypeSceneBuilder.cs` | 菜单自动生成场景、Prefab、配置资产、占位图和物理材质，自动绑定全部引用。 |
| `Tests/Editor/PrototypeSmokeChecks.cs` | 在独立 Unity 2D 物理场景检查选择限制、弹射锁定、碰撞传递、减速与恢复输入。 |

## Unity 编辑器操作

1. 等待 Unity 导入脚本、完成编译。
2. 在 Project 面板双击 **`Assets/_Game/Scenes/SC_Battle.unity`**，单独打开该场景。如 Unity 提示保存原场景，请按你自己的需要保存，不要丢弃已有修改。
3. 如果该场景尚未生成，点击 **`Tools > Tuanzi > Build Prototype Scene`**。该工具会保存新场景并在 Project 面板选中它，再双击打开。所有现有场景及未保存内容会保留。
4. 点击 **Play**。正常应看到深色矩形场地、左侧三个青绿色团子、右侧三个粉色团子。
5. 按住一个青绿色团子的鼠标左键，向后拖动，松开时向箭头方向弹射。向左拉会向右发射。

工具重复执行时，已有资源不会被覆盖，新场景和资源会自动增加编号。无需手动拖引用，也不会修改 Build Settings、输入设置或现有 SampleScene。

如果当前有未保存的 **Untitled** 场景，请先通过 `File > Save As...` 保存再使用搭建工具；Unity 不允许在这种状态下追加新场景。

### 生成内容与对象绑定

```text
SC_Battle
├── Main Camera                  Camera + AudioListener
├── Arena
│   ├── Background               SpriteRenderer，无碰撞体
│   ├── CenterLine               SpriteRenderer，无碰撞体
│   └── TemporaryWall_*          SpriteRenderer + BoxCollider2D，共四面
├── Units
│   ├── Player_Heavy / Elastic / Support
│   └── Enemy_Heavy / Elastic / Support
├── GameSystems
│   ├── PrototypeSession         六个 Units 引用
│   └── PlayerInputController    Camera、PrototypeSession、AimPreview 引用
├── AimPreview                   AimPreview + LineRenderer
└── EventSystem                  EventSystem + StandaloneInputModule
```

相比正式场景草案，本阶段保留三项最小交互系统；完整回合、战斗、连携、AI 和 Canvas 留待对应阶段实现。EventSystem 仅用于阻止 UI 点击穿透，为后续 UI 留接口。

共用 Prefab：**`Assets/_Game/Prefabs/Units/PF_DangoUnit.prefab`**。

```text
PF_DangoUnit
├── Rigidbody2D + CircleCollider2D + UnitActor
├── Visual                       SpriteRenderer，内含两只占位眼睛
└── SelectionIndicator           SpriteRenderer，默认隐藏
```

配置资产：`Assets/_Game/ScriptableObjects/Units/UD_Heavy.asset`、`UD_Elastic.asset`、`UD_Support.asset`。
图片在 `Art/Characters` 和 `Art/Arenas`，材质在 `Materials`。它们均由工具生成，可以替换占位美术。

### Inspector 参数

| 对象 / 组件 | 参数 | 默认值 / 绑定 |
| --- | --- | --- |
| Main Camera / Camera | Projection、Size、Position | Orthographic、4.6、(0, 0, -10) |
| PlayerInputController | World Camera | Main Camera |
| PlayerInputController | Session | GameSystems/PrototypeSession |
| PlayerInputController | Aim Preview | 场景根对象 AimPreview |
| PlayerInputController | Max Drag Distance | 2 世界单位 |
| PlayerInputController | Impulse Per Unit | 5，满蓄力基础冲量为 10 |
| PlayerInputController | Min Drag Distance | 0.15 世界单位 |
| PlayerInputController | Selectable Layers | Everything；检测后再按 UnitActor 阵营过滤 |
| PrototypeSession | Units | 场景中的六名角色 |
| PrototypeSession | Settled Speed / Duration | 0.2 单位/秒，连续 0.5 秒 |
| PrototypeSession | Maximum Resolve Time | 8 秒；仅允许清除速度不超过 0.4 的残余运动 |
| 各角色 / UnitActor | Data | 对应职业的 UD_* 资产 |
| 各角色 / UnitActor | Team | Player 或 Enemy |
| 各角色 / UnitActor | Visual / Selection Indicator | 同名子物体的 SpriteRenderer |
| 各角色 / Rigidbody2D | Body Type / Gravity Scale | Dynamic / 0 |
| 各角色 / Rigidbody2D | Collision Detection / Interpolate | Continuous / Interpolate |
| 各角色 / Rigidbody2D | Constraints | Freeze Rotation Z |
| 各角色 / CircleCollider2D | Radius | 0.44；根对象缩放为 (1, 1, 1) |
| UD_Heavy | Mass / Linear Damping / 弹性 | 1.6 / 1.5 / 0.5 |
| UD_Elastic | Mass / Linear Damping / 弹性 | 0.9 / 1.5 / 0.9 |
| UD_Support | Mass / Linear Damping / 弹性 | 1.1 / 1.5 / 0.7 |

物理参数在进入场景时由 UnitData 应用到刚体；Unity 2022 Inspector 中阻尼属性名为 **Linear Drag**。编辑 UnitData 后重新进入 Play 生效，不要仅修改场景刚体的 Mass/Drag，否则下次初始化会重新读取配置。不同质量的角色在相同冲量下速度不同。

出生位置：我方 X=-1.8，敌方 X=1.8；从上到下 Y=2、0、-2。场地内部范围 X=[-5.5,5.5]，Y=[-3.5,3.5]。

## 验证步骤

先打开 SC_Battle，再运行 **`Tools > Tuanzi > Validate Prototype Physics`**。自动检查通过时 Console 输出 `TUANZI_SCENE_CHECKS_PASSED` 和 `TUANZI_PHYSICS_CHECKS_PASSED`。检查包括六人阵营分配、资产及脚本引用，以及真实 Unity 2D 物理模拟；不代表鼠标交互验收已完成。

本次已在本机同版本 Unity 的隔离临时项目中完成编译、场景生成、上述自动检查，并渲染 1280×720 预览图检查六名角色和场地显示。记录见 `Tests/PrototypeValidationReport.txt`。没有自动操作 Game 窗口的鼠标，以下交互仍需你验收。

进入 Play 后逐项验收：

1. 六名角色均显示且初始不下落、不重叠；左侧青绿色、右侧粉色。
2. 按住左侧任意角色，拖动时显示选中圈和箭头，角色本体不跟随鼠标移动。
3. 向左后拉，虚线向右；拖得越远预览越长，拖拽达到 2 单位后蓄满，辅助路径总长默认最多 6 单位；遇障碍最多显示一次反弹。
4. 松手，角色向箭头方向弹射。对准同一行敌方并满蓄力，可观察碰撞传递和自然减速。
5. 发射后运动期间点击其他我方团子，不得再次弹射；所有团子停稳后可继续选择任意我方团子。
6. 单击不拖动、拖动不足 0.15 单位、点击空地、点击敌方，都不得产生弹射。
7. 瞄准中按 Esc 或鼠标右键取消；切换到其他应用也会取消。重新回来不会意外发射。
8. 在 Game 窗口外松开左键应取消弹射。重新进入窗口后可以正常操作。
9. 角色撞到临时围栏后仍留在视野内；击中队友同样有物理反馈。
10. 停止 Play 再进入，六名角色回到初始位置，Console 不应出现新增错误。

若项目以后改用新 Input System，此版鼠标脚本仍需旧输入 API：`Edit > Project Settings > Player > Other Settings > Active Input Handling` 设为 **Input Manager (Old)** 或 **Both**。当前项目已是旧输入，无需更改。

## 已知问题与下一阶段

- 本次按任务书只完成阶段一。敌方没有 AI，不会主动发射；尚未实现每人每轮一次的限制、轮次切换、生命值、伤害、击倒、胜负、连携、音效、正式 UI。
- 当前四面围栏只用于保留测试角色；正式设计要求的悬崖和场外击倒在场地阶段替换。
- 使用圆形占位角色，未导入正式美术。摄像机为固定正交尺寸，建议使用 16:9 Game 视图。
- 本阶段可反复弹射任何己方角色，不消耗回合；物理停稳锁定仅用于避免运动中重复输入。
- 自动物理检查不能代替鼠标交互、选中圈、箭头和窗口失焦的 Play 模式人工验收。
- 验收通过并收到你的反馈后，再进入生命值和碰撞伤害阶段。

API 参考：[Unity 2022.3 Rigidbody2D.AddForce](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rigidbody2D.AddForce.html)、[Rigidbody2D.drag](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rigidbody2D-drag.html)。
