# 三档敌方 AI 与交替回合

打开现有 **Assets/Scenes/SC_Battle.unity**，进入 Play 即可。系统自动补齐 GameFlowController 与 AIController，无需重建场景或手动绑定。

## 行动顺序

玩家先手：选一名未行动的我方角色弹射 → 六个角色停稳 → AI 思考并弹射一名敌方角色 → 六个角色停稳 → 再次允许玩家选择。

每名角色每大轮最多主动弹射一次，被动受撞移动不消耗行动机会。死亡后满血回到本局初始出生点，使用记录不变。敌我六名角色都各主动发射一次并完成结算后，才进入下一大轮，重置行动标记，玩家先手。暂时死亡不会移除本轮应行动的角色或提前重置大轮。

已取消全灭胜负判定，所有角色都可以反复复活。本阶段不计分、不判输赢。未使用我方角色有金色旋转箭头圆环；发射后消失，死亡不改变，新大轮恢复。详见 `README_复活与行动标记.md`。

## 难度

| 枚举 / UI 下拉索引 | 瞄准方式 | 基础冲量比例 |
| --- | --- | --- |
| Easy / 0 | 随机选择可行动敌人和我方目标，方向默认带 ±22° 误差 | 随机 30%～50%，最多 50% |
| Medium / 1 | 从可行动敌人中选择较近的我方目标，直瞄角色中心 | 随机 45%～60%，最多 60% |
| Hard / 2 | 直瞄角色中心 | 固定 100% |

中高档仅按当前目标方向直射，不额外预测墙面反射或规划绕行。

比例以 **PlayerInputController.MaxDragDistance × ImpulsePerUnit** 为 100%，会读取当前场景的实际参数。当前场景为 2 × 8 = 16，因此 Easy 最多 8、Medium 最多 9.6、Hard 为 16。之后调整玩家力度，AI 同步调整。

该比例指施加到 UnitActor 前的基础冲量；各职业仍使用同样的 UnitData.LaunchMultiplier 和质量，因此不同职业的实际速度可以不同。即使外部脚本提交过大的冲量，统一发射入口仍会按当前难度上限截断。

## 调整默认难度

在 Project 中选择 **Assets/_Game/Resources/AISettings.asset**：

- **Default Difficulty**：默认 Medium。
- **Think Delay**：默认 0.6 秒，控制敌方停稳后的思考延迟。
- **Easy Aim Error Degrees**：默认 22°，控制低档瞄准误差。

默认值在新对局初始化时加载。暂停和顿帧期间不会推进 AI 思考；物理结算完成前 AI 不会发射。

## 预留 UI 接口

最方便的方式：将现有 **GameSystems/PrototypeSession** 拖到未来的 Dropdown/TMP_Dropdown 的 `On Value Changed (Int32)`，选择动态整数方法 **PrototypeSession.SetAIDifficulty(int)**。选项顺序为 Easy、Medium、Hard。

也可以通过脚本调用：

```csharp
using TuanziArena;

// UI 下拉框传入 0/1/2。
session.SetAIDifficulty(dropdownIndex);

// 或使用枚举。
session.Flow.SetDifficulty(AIDifficulty.Hard);

// 读取当前状态。
AIDifficulty difficulty = session.Flow.Difficulty;
BattleState state = session.Flow.State;
int round = session.Flow.RoundNumber;
```

运行中修改难度会影响**下一次 AI 发射**；不会修改正在飞行的角色。AI 思考期间修改也会在发射前使用最新难度。默认配置资产不会因 UI 调整而被改写。

GameFlowController 同时提供 C# 事件：

- `DifficultyChanged(AIDifficulty)`：难度变化。
- `StateChanged(BattleState)`：BattleSetup、PlayerSelect、AIThinking、PhysicsResolving 状态变化。
- `RoundChanged(int)`：轮次变化。

UI 初次绑定时先读取上述属性，再订阅事件。`session.Flow.Turns.HasActed(unit)` 可用于显示已行动状态，`Turns.EligibilityChanged` 通知使用标记变化；`session.CanSelectUnit(unit)` 可用于判断当前是否允许操作该角色。计分与结束对局接口待后续规则明确后再添加。

## 新增或修改脚本

| 文件（相对 Assets/_Game） | 职责 |
| --- | --- |
| Scripts/AI/AIDifficulty.cs | 稳定的三档枚举，对应 UI 索引。 |
| Scripts/AI/AIShotPlanner.cs | 选择可行动敌方与存活目标，计算误差和受限力度。 |
| Scripts/AI/AIController.cs | 思考计时，通过统一发射入口执行一次 AI 行动。 |
| Scripts/Core/TurnManager.cs | 记录每轮已行动角色、判断剩余资格、重置轮次。 |
| Scripts/Core/GameFlowController.cs | 控制行动方、切换回合，等待完整名册全部行动，提供 UI API 和事件；不按全灭结束对局。 |
| Scripts/Core/PrototypeSession.cs | 保留物理停稳检测、统一发射与行动方伤害来源，接入流程控制器。 |
| Scripts/Input/PlayerInputController.cs | 提供实际满蓄力冲量，注册给 Session；原有鼠标拖拽流程继续使用。 |
| Scripts/Data/AISettings.cs、Resources/AISettings.asset | 默认难度、思考延迟与低档误差配置。 |
| Tests/Editor/TurnAndAIChecks.cs | 验证难度边界、瞄准、六次行动的大轮、复活后资格保持与旋转环显示。 |
| Tests/Editor/PrototypeSmokeChecks.cs | 更新为验证玩家/敌方物理结算交接，兼容当前场景位置。 |

运行时自动创建的 GameFlowController 与 AIController 组件位于 GameSystems/PrototypeSession 对象上。无需更改当前场景中的角色、出生点或已有力度参数。

## 验收步骤

1. 进入 Play，确认最初只能选择我方，敌方不会抢先行动。
2. 弹射一名我方，运动中不能重复发射；停稳后等待约 0.6 秒，敌方应自动弹射一次。
3. 敌方运动和思考期间不能操作我方；敌方停稳后可以选择我方下一名未行动角色。
4. 本轮已主动行动的角色不能再次选择；双方三名均完成行动后进入新轮，可再次选择第一名角色。
5. 分别使用三档难度测试。运行时可在自动创建的 GameFlowController 组件中修改 Difficulty，或调用预留 API。
6. 确认敌我行动期间的单向伤害、墙角连续碰撞、血量 UI、瞄准线和强碰撞反馈仍正常。
7. 击倒尚未行动的角色，检查回出生点后仍可行动；击倒已行动角色，检查复活后仍不可在本轮再次行动。
8. 同时击倒一方或全部角色，检查满血复活、对局继续，大轮不会因死亡提前重置。

退出 Play 后可运行 **Tools > Tuanzi > Validate Turns And AI** 进行自动逻辑检查。
