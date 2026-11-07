# 接力玩法

直接运行现有 `Scenes/SC_Battle.unity`，无需重新搭建场景。`PrototypeSession` 自动创建并配置 `RelayController`。

## 规则

- 只在**我方行动**的物理运动过程中，我方两名存活角色发生新的碰撞接触，且**相对碰撞速度大于 3**时触发。敌方互撞、敌我碰撞、墙壁、低速接触和持续贴住不会触发。
- 每一大轮共 **3 次**，双方六名角色各行动一次之后重置；换边、下一名角色行动、死亡或复活不重置次数。
- 触发即消耗一次额度，即使没有操作也消耗。
- 战场暂停 **2 秒真实时间**。原有 UI 隐藏，底部接力 UI 显示头像、倒计时、剩余次数和鼠标键位提示。橙色进度条从左右两端同时向中心缩短至零。
- 左右角色按**碰撞瞬间的 X 坐标**固定，完全相同则随机分配，不按主动发射者或碰撞回调顺序排列。头像直接读取现有 `UnitActor.HeadSprite`；未配置时回退到角色身体 Sprite。
- 鼠标移动决定方向：箭头从碰撞瞬间的两球中点指向鼠标，可配置显示长度上限。鼠标过近而无法确定方向时忽略点击；鼠标在游戏视口之外也不接收点击。
- **左键**给左侧头像对应的角色追加冲量，**右键**给右侧角色追加冲量，点击后立即恢复时间、隐藏接力 UI、还原其它 UI。
- 不操作则到期直接恢复，保留原本速度。追加推力属于当前行动，不修改 ActionId、当前攻击阵营或六名角色的已使用标记，也不重置总伤害。
- 新的高速接触可以在剩余额度内再次触发；同一次接触的 Stay 不触发。达到三次后，本大轮继续正常运动。
- 死亡特写优先于接力，重开或禁用接力组件会恢复其持有的暂停和 UI 状态。

## 参数与独立 UI

配置资产：`_Game/Resources/RelaySettings.asset`。

| Inspector 参数 | 默认 | 用途 |
| --- | --- | --- |
| Enable Relay | 开 | 接力总开关 |
| Minimum Impact Speed | 3 | 触发相对速度阈值，必须严格大于 |
| Max Triggers Per Round | 3 | 每大轮最多触发次数 |
| Countdown Seconds | 2 | 不受 Time.timeScale 影响的等待秒数 |
| Impulse | 8 | 追加冲量；速度增量取决于刚体质量 |
| Preserve Velocity | 开 | 保留原速度并追加冲量；关闭则先清零所选角色速度 |
| Minimum Aim Distance | 0.1 | 鼠标到中点的最小有效距离 |
| Arrow Max Length | 3 | 箭头的最大世界长度，不影响冲量大小 |
| Arrow Width / Head Length / Color | 0.07 / 0.28 / 橙色 | 箭头样式 |

UI 预制体：`_Game/Resources/UI/PF_RelayUI.prefab`，双击即可修改布局、头像尺寸、字体、颜色等。
进度条为 `SafeArea/RelayPanel/ProgressBounds/OrangeProgress`，运行时只修改横向锚点来居中收缩。
`LeftPortrait` / `RightPortrait` 下的 `CircleMask/HeadSprite` 自动接收头像，不需要逐个角色绑定 UI。
场景无需手动放入预制体；管理器从 Resources 加载一次。

`Tools > Tuanzi > Create Relay Assets` 可补建缺失资产，不覆盖已经编辑过的配置或预制体。
所有 UI 图形均为现有 UGUI 图元绘制，沿用站酷快乐体，无新增外部字体或图片依赖。

## 接口与验证

- `session.Relay.IsActive / IsBusy`：当前是否正在读秒 / 包括等待处理的碰撞请求。
- `UsedThisRound / RemainingTriggers / RemainingSeconds`：次数与剩余时间。
- `LeftUnit / RightUnit / Midpoint`：当前接力选项和方向原点。
- `TryChoose(0, mouseWorld)` / `TryChoose(1, mouseWorld)`：供其它输入方式接入，成功后立即结束接力。
- `Cancel()`：取消当前读秒并恢复 UI/时间，不退还已触发的额度。

`Tests/Editor/RelayChecks.cs` 提供隔离工程批处理验证入口 `TuanziArena.Editor.RelayChecks.RunBatch`。它会切换 Play 并退出 Unity，应只在测试副本运行。
实际运行截图为 `Tests/RelayPreview.png`，检查结果见 `Tests/RelayValidationReport.txt`。
