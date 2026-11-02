# 角色攻击力、生命值与底部 UI

直接打开 `Assets/Scenes/SC_Battle.unity`，进入 Play。每名角色下方显示两个带描边的深蓝色卡片：黄色闪电代表攻击力，红色爱心代表当前血量。卡片跟随角色，不拦截鼠标瞄准。

## 初始数值

双方使用同一套配置，但当前血量分别保存。

| 配置资产（Assets/_Game/ScriptableObjects/Units） | 攻击力 | 最大血量 |
| --- | --- | --- |
| UD_Heavy.asset | 10 | 65 |
| UD_Elastic.asset | 8 | 45 |
| UD_Support.asset | 6 | 50 |

在 Project 中选中对应资产，修改 **Attack Power** 和 **Max Health**，重新进入 Play 即可使用新数值。修改一个职业配置会影响双方该职业的初始属性；受伤只扣当前角色的血，不会改动共享配置或另一名角色的血量。

## 伤害与击倒

伤害归属由本次行动方决定，并持续到所有角色停稳：

- 我方行动期间，任意我方球与敌方球接触，只扣敌方血，我方不会受伤。
- 敌方行动期间，任意敌方球与我方球接触，只扣我方血，敌方不会受伤。
- 不要求碰撞者是最初弹射的角色；被推走的队友同样能造成伤害。双方速度大小和回弹方向不会改变伤害归属。
- 队友之间、敌方内部以及球与墙壁接触都不直接扣血。
- 两球真正分离后再次接触立即再次扣血，没有时间冷却；因此可利用墙角反弹连续攻击。
- 持续贴住不按物理帧反复扣血，同次接触的 Enter/Stay 与双方回调只结算一次。新行动开始时，已经贴住的敌我角色也可以结算该次行动的首次接触伤害。

```text
速度系数 = Clamp(相对速度 / 4, 0.5, 2)
伤害 = Clamp(RoundToInt(攻击力 × 速度系数), 4, 20)
```

伤害使用实际接触的行动方角色的攻击力。例如攻击力为 10、相对速度为 4 时造成 10 点伤害；低速接触也造成伤害，速度只影响数值，不影响哪一方扣血。攻击力为 0 时不造成伤害。

血量降至 0 时，当前角色变暗，数字显示红色 0，停止运动、关闭物理模拟和圆形碰撞体，并失去被选择与弹射的资格。击倒事件只发送一次。本阶段保留变暗的角色和 0 血量标记，便于观察；重新进入 Play 重置全部角色。

伤害系统会由 PrototypeSession 自动初始化，无需手动拖引用。如需持久修改伤害数值规则，可在退出 Play 后选中 **GameSystems/PrototypeSession**，通过 **Add Component > Collision Damage System** 添加组件并保存场景。该组件中的 Reference Impact Speed、Minimum/Maximum Damage 控制上述数值；已有组件会被自动使用。旧的最低速度门槛和 0.25 秒碰撞冷却已移除。

玩家鼠标入口仍是 `PrototypeSession.TryLaunch`。未来 AI 可通过同一系统的 `TryLaunchForTeam(unit, impulse, UnitTeam.Enemy)` 启动敌方行动。系统在弹射成功时记录 ActiveTeam 与 ActionId，并在停稳后清空行动方；原始弹射角色即使中途击倒，也不改变本次行动的伤害归属。

## UI 和 Prefab

`PF_DangoUnit.prefab` 已增加 UnitCollisionReporter、UnitStatsUI，以及以下子层级：

```text
PF_DangoUnit
└── UnitStatsUI                 World Space Canvas，缩放 0.01
    ├── AttackBadge             深蓝底框
    │   ├── AttackIcon          黄色闪电，白描边
    │   └── AttackValue         白色攻击力数字
    └── HealthBadge             深蓝底框
        ├── HealthIcon          红色爱心，白描边
        └── HealthValue         当前血量数字
```

图标与卡片由 StatIconGraphic 生成 UI 网格，无需外部贴图。数字使用 Unity 内置字体。UnitStatsUI 的 Local Offset 默认 (0,-0.65)，控制生成 UI 时相对角色的位置；已有 UI 可直接调整 UnitStatsUI 子对象的 Transform。血量低于或等于 30% 时数字变黄。

现有场景通过共用 Prefab 自动获得 UI，无需重建场景。运行时也会为缺少 UI/碰撞转发组件的场景角色补齐组件。以后重新生成场景，场景搭建工具同样会创建这些内容。

## 新增或修改的文件

| 文件（相对 Assets/_Game） | 职责与关联 |
| --- | --- |
| Scripts/Data/UnitData.cs | 保存最大血量与基础攻击力配置。 |
| Scripts/Units/UnitActor.cs | 保存当前血量，处理伤害、状态变化和一次性击倒事件。 |
| Scripts/Units/UnitCollisionReporter.cs | 转发接触开始、持续和分离；同一碰撞对只由一名转发器负责。 |
| Scripts/Combat/CollisionDamageSystem.cs | 按行动阵营确定单向伤害，跟踪接触与分离，支持无时间冷却的重复碰撞。 |
| Scripts/UI/UnitStatsUI.cs | 构建角色底部 UI，订阅角色事件更新数字。 |
| Scripts/UI/StatIconGraphic.cs | 绘制闪电、爱心和圆角卡片。 |
| Scripts/Core/PrototypeSession.cs | 初始化伤害系统，保存当前行动阵营、行动编号与弹射来源。 |
| Editor/PrototypeSceneBuilder.cs | 为新场景生成战斗属性与角色 UI。 |
| Editor/UnitStatsSetup.cs | 菜单 Tools > Tuanzi > Upgrade Unit Stats Prefab，仅补齐角色 Prefab 组件与 UI。 |
| Tests/Editor/CombatChecks.cs | 菜单 Tools > Tuanzi > Validate Unit Combat Stats，验证扣血、UI 更新、去重与击倒。 |

同时更新三份 UD_* 配置资产及共用角色 Prefab。

## Play 验收步骤

1. 进入 Play，确认六名角色下方都显示闪电/攻击力和爱心/血量，数值与职业配置一致。
2. 用上排我方重锤团子向右满蓄力直射敌方，观察敌方血量减少且只扣一次，数字随角色移动。
3. 碰撞我方队友，确认双方都不扣血；让该队友再撞敌方，确认仍只扣敌方血。
4. 反复撞击敌方，观察血量变黄、归零、角色变暗并退出物理碰撞。
5. 瞄准已归零的角色，不应再把它识别成障碍；已归零的我方角色不能选择或发射。
6. 停止并再次进入 Play，确认角色恢复最大血量。

自动检查包括双方行动伤害归属、非发射队友造成伤害、低速接触扣血、快速分离再接触、持续接触去重、换行动后重置接触记录，以及原始发射角色击倒后的行动方保持。

本次仅增加战斗属性和角色底部 UI；当前仍为自由弹射测试流程，敌方尚未接入 AI，未增加回合与胜负界面。
