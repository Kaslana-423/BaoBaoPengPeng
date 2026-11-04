# 伤害飘字与本次总伤害

直接运行现有战斗场景，无须重新搭建。`CollisionDamageSystem` 会自动接入伤害表现并加载 UI。

## 显示规则

- 每次有效碰撞扣血，在受伤角色的位置弹出 `-数值`，先放大弹跳，再上浮、淡出。默认使用圆润的 **Lilita One** 卡通数字字体。
- 我方造成的伤害为金黄色，敌方造成的伤害为粉红色；字体和颜色均可替换。
- 数字与总伤害均按**实际扣除血量**计算。例如目标剩 3 血、这次伤害为 20，显示 `-3`，累计 3。
- 同一次持续接触只飘一次；分离后再次碰撞会重新飘字，保留墙角连续碰撞机制。同一次行动内，被动撞击的其他队友也计入总伤害。
- 致死飘字记住受击位置，不会跟随角色跳回出生点。连续命中使用错开的三条上浮路径。
- 顶部比分下方的“我方/敌方 · 总伤害”默认隐藏。本次行动第一次造成伤害后显示并累计；行动物理结算结束时立即隐藏，进入比分弹窗或下一方选择/思考阶段也不会保留。敌我规则相同，没有造成伤害的行动全程不显示。
- 下一次成功发射清零，取消拖拽、发射失败不会清零。统计数据可保留到下一次发射，但行动结束后不再显示面板。
- 每次命中，总伤害数字快速递增并弹跳，背景播放金色扫光、放射光和星星闪烁。可关闭闪光或调整强度。
- 飘字、数字递增和闪光均使用不受时间倍率影响的时间，碰撞短时停不会被误判为行动结束；行动结束后总伤害面板隐藏，已经生成的独立飘字仍正常淡出。不会修改游戏的暂停状态。
- 伤害 UI 不接收鼠标射线，不影响选球与拖拽。比分弹窗和结算遮罩会显示在它上方。
- “再来一局”清空总伤害和残留飘字，不重复生成 UI。

## 调整字体、边框与动画

在 Project 中选中 **`_Game/Resources/DamageFeedbackSettings.asset`**，在 Inspector 调整：

| 参数 | 用途 |
| --- | --- |
| Number Font / Number Font Size | 飘字字体与字号；总伤害也使用同一数字字体 |
| Player Damage Color / Enemy Damage Color | 我方/敌方造成伤害的数字颜色 |
| Outline Width / Outline Color | 飘字描边粗细、颜色；粗细为 0 关闭描边 |
| Number Lifetime / Fade Start | 飘字持续秒数、开始淡出的生命进度 |
| Rise Distance / Horizontal Drift | 上浮距离与左右漂移距离，单位为参考画布像素 |
| World Offset | 受击位置上方的偏移，单位为世界坐标 |
| Pop Scale / Number Separation | 弹跳最大倍率、连续飘字的横向间距 |
| Maximum Numbers | 同屏飘字上限，超出时复用最早的数字，避免持续创建对象 |
| Total Position / Total Scale | 总伤害面板相对屏幕安全区域顶部中心的位置与缩放 |
| Total Font Size / Total Number Color | 总伤害数值的字号与颜色 |
| Total Outline Width / Total Outline Color | 总伤害数字独立的描边粗细与颜色 |
| Label Font / Panel Color | 中文标题字体、面板背景颜色 |
| Enable Flashes / Flash Color / Flash Duration / Flash Intensity | 闪光开关、颜色、时长与强度 |
| Total Punch Scale | 总伤害面板受到命中时的弹跳幅度 |
| Show Floating Numbers / Show Total Damage | 分别开关飘字与总伤害面板 |

这些参数可在运行时通过 Inspector 观察调整效果。字体与描边由配置资产统一应用；修改字体请改这里，不要只改预制体 Text 的 Font。

## 独立 UI 资产

- `_Game/Resources/UI/PF_DamageNumber.prefab`：单个飘字，包含 Text、Outline、CanvasGroup。
- `_Game/Resources/UI/PF_DamageFeedbackUI.prefab`：飘字画布与总伤害面板。背景扫光在 `SafeArea/TotalDamage/Punch/ShineClip`，星光为 `Sparkle_*`，放射光为 `Burst`。
- `_Game/Art/Fonts/LilitaOne-Regular.ttf` 与 `LilitaOne-OFL.txt`：开源数字字体与许可。

不必拖预制体到场景，系统会自动实例化一次。顶部位置默认适配现有比分；移动比分以后，可通过 `Total Position` 调整总伤害位置。

## 代码与验证

- `CollisionDamageSystem.DamageDealt(DamageHit)`：只在有效碰撞扣血后发送一次；携带攻击者、受击者、实际伤害、行动 ID 和受击位置。
- `PrototypeSession.ActionStarted(UnitActor, int)`：成功发射后触发，供总伤害清零使用。
- `PrototypeSession.ActionEnded()`：行动物理结算结束、切换流程前触发，供总伤害面板立即隐藏。
- `DamageFeedbackController.TotalDamage` / `HitCount` / `ActingTeam`：读取当前行动的伤害统计；`TotalDamageChanged(int)` 可供后续 UI 订阅。
- 飘字使用对象池，默认预热 8 个，同时最多显示 40 个。
- `Tools > Tuanzi > Validate Damage Feedback`：检查实际血量、致死伤害、重复碰撞去重、同队被动伤害累计、敌我切换及重开。
- `Tools > Tuanzi > Create Damage UI Assets`：补建缺失资产，不覆盖已有配置和预制体。
