# 比分、得分弹窗与六分结算

直接运行现有 `Scenes/SC_Battle.unity`。`PrototypeSession` 会自动接入计分系统并实例化下面两个预制体，无须重新搭建场景。

## 规则与时序

- 蓝色是我方，红色是敌方；每击杀对方一次得 **1 分**，顶部比分立即更新。
- 保留原有机制：行动方的所有小球都能通过碰撞伤害对方；死亡后仍回到初始出生点满血复活，使用次数不改变。同一目标复活后再次被击杀可以再次得分。
- 每次主动发射视为一次“行动”。本次行动的物理运动、复活结束后，如果有得分，双方暂停，统一播放一次得分动画。多杀显示累计 `+N`，不会每击杀一次就打断碰撞。
- 弹窗从下方弹入，显示更新后的比分和六个团子得分标记，停留后向下收回。动画使用 `Time.unscaledDeltaTime`，暂停不会卡住 UI。
- 无击杀的行动直接换边。非获胜得分动画结束后，恢复原来的时间倍率并进入应当行动的一方；每六个角色各用一次才开启下一大轮。
- **先达到 6 分的一方获胜**。达到六分时锁定胜方，停止继续计分/碰撞伤害；当前运动收尾后先展示比分，再弹出胜利或失败界面。游戏保持暂停，不再开启下一行动或大轮。
- 比分弹出、收回、结算期间，我方行动圆环隐藏，鼠标发射和敌人 AI 均锁定。
- “再来一局”在当前场景重开：比分归零、满血、回到初始出生点、行动次数清空、大轮回到 1、我方先手。保留已选 AI 难度和当前发射力度配置。

## 可单独编辑的 UI 资产

| 资产 | 用途 |
| --- | --- |
| `_Game/Resources/UI/PF_BattleScoreUI.prefab` | 顶部常驻比分和得分动画弹窗 |
| `_Game/Resources/UI/PF_BattleResultUI.prefab` | **独立胜利/失败结算界面**，包含遮罩、星星、标题、最终比分、重开按钮 |

双击预制体进入 Prefab Mode，直接调整子节点的布局、颜色和文字。结算预制体保存的是可见的胜利示例，运行时会先隐藏，根据胜负更新标题和配色；不需要拖到战斗场景，系统会自动加载一次。

比分预制体中的 `ScorePopup` 默认关闭，编辑弹窗时可以临时激活；运行时脚本会重新设置显隐。`HUD` 是顶部比分，`ScorePopup/AnimatedPanel/ScoreRibbon` 是大比分。结算的主要内容在 `ResultOverlay/ResultCard`。

`BattleScoreUI` Inspector 可调 `Enter Seconds`（默认 0.32 秒）、`Hold Seconds`（1.05 秒）、`Exit Seconds`（0.28 秒）。`BattleResultUI.Enter Seconds` 默认 0.42 秒。所有节点、文字和脚本引用均已序列化到预制体。

Canvas 使用 1280×720 参考分辨率和 Expand 缩放，适配横屏和竖屏，顶部 HUD 考虑屏幕安全区域。常驻比分不拦截鼠标，弹窗/结算遮罩拦截点击。

图形为代码绘制的双色条带、团子标记和星星，无需额外图片。中文使用开源 **站酷快乐体 ZCOOL KuaiLe**；字体及 SIL OFL 许可保存在 `_Game/Art/Fonts/`，可替换预制体 Text 的 Font。

## 脚本职责与接口

- `BattleScoreController`：监听角色死亡、计分、锁定胜方、暂停管理、串联比分和结算。`PlayerScore` / `EnemyScore` / `Winner` 只读，`WinningScore = 6`。
- `GameFlowController`：新增 `ScorePresentation`、`BattleEnded` 两个状态，避免动画期间 AI 抢先行动。
- `BattleScoreUI`：顶部比分和得分动画；`ScoreRibbonView`：数字和六个团子标记。
- `BattleResultUI`：独立结算视图，可用 `Show(bool playerWon, int player, int enemy)` 预览不同结果，用 `Hide()` 隐藏。单独调用视图不会修改比赛状态或时间。
- `PrototypeSession.RestartBattle()`：重开接口，已绑定结算按钮。
- `session.Flow.Score.ScoreChanged(int player, int enemy)`：每次有效得分后通知。
- `session.Flow.Score.BattleFinished(UnitTeam winner)`：进入胜负结算时通知一次。

比分暂停前会主动结束 `ImpactFeedback` 的短时停，避免它的恢复计时器误解除比分暂停。重开或卸载战斗时会释放本系统持有的暂停。

## 验证入口

`Tools > Tuanzi > Validate Score Rules` 检查双方计分、每条生命只计一次、复活重复击杀、六分封顶、重开、无得分跳过动画。原有回合、碰撞、瞄准验证入口保留。

`Tools > Tuanzi > Create Score UI Assets` 可补建丢失的 UI 预制体，已有预制体不会被覆盖，保护手动编辑的布局。
