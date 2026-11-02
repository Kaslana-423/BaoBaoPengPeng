# 强碰撞反馈：顿帧与震屏

强碰撞加入短顿帧和衰减震屏。已有场景直接进入 Play 即可，碰撞伤害系统会自动添加 ImpactFeedback，无需重新搭建场景或绑定摄像机。

角色撞到我方、敌方或墙壁都可以触发反馈；只有接触开始时检查强度，持续贴住不会重复触发。碰撞越强，震屏幅度越大。视觉反馈不施加额外冲量，也不改变攻击力、质量、阻尼或行动方伤害规则。

## 调整位置

在 Project 面板选择 **Assets/_Game/Resources/ImpactFeedbackSettings.asset**。这份配置会自动加载，退出 Play 后调整可长期保存。

| 参数 | 默认值 | 作用 |
| --- | --- | --- |
| Minimum Impact Speed | 2 | 最小触发相对速度；调低可让更多碰撞触发 |
| Full Impact Speed | 8 | 达到该速度时使用最大震幅 |
| Enable Hit Stop | 开 | 是否启用短顿帧 |
| Hit Stop Seconds | 0.045 | 真实时间停顿，约 45 毫秒；代码最多允许 0.12 秒 |
| Enable Shake | 开 | 是否启用震屏 |
| Shake Seconds | 0.14 | 震屏持续时间，随时间快速衰减 |
| Minimum Shake Distance | 0.03 | 刚达到阈值时的震幅，单位为世界单位 |
| Maximum Shake Distance | 0.12 | 最大震幅 |
| Shake Frequency | 35 | 震动频率参数 |
| Feedback Cooldown | 0.12 | 两次反馈之间的最短真实时间间隔，仅限制反馈 |

想更重一点，可以把 Hit Stop Seconds 调到 **0.06**、Maximum Shake Distance 调到 **0.16**。想更利落，可以把顿帧降到 **0.03**。这些是本项目的初始调试值，最终以实际 Play 手感为准。

墙角连撞仍按每次真正分离再接触扣血。Feedback Cooldown 不会限制伤害；它只是防止密集碰撞让画面连续冻结。顿帧使用真实时间恢复，不修改刚体速度；震屏只添加摄像机临时偏移，结束后移除。组件停用、销毁或窗口失焦时也会清理反馈。

## 文件

- `Scripts/Data/ImpactFeedbackSettings.cs`：配置类型。
- `Resources/ImpactFeedbackSettings.asset`：实际默认参数。
- `Scripts/Combat/ImpactFeedback.cs`：真实时间顿帧、镜头震动和清理恢复。
- `Scripts/Combat/CollisionDamageSystem.cs`：自动初始化反馈、验证当前是否正在行动。
- `Scripts/Units/UnitCollisionReporter.cs`：在角色/墙面碰撞开始时上报强度，同一角色碰撞对只上报一次。

## Play 验收

1. 打开现有 SC_Battle，进入 Play，用弹力团子满蓄力正面撞敌方，观察短暂的撞击停顿和快速震屏。
2. 小力度拖动碰撞，低于触发速度时不应顿帧或震屏。
3. 强力撞墙和撞队友也有反馈；友方与墙面依然不直接扣血。
4. 测试墙角多次反弹，伤害可以连续触发，反馈不会每个物理帧都触发。
5. 在 Play 中查看 GameSystems/PrototypeSession 上自动生成的 Impact Feedback 组件。碰撞结束后游戏速度恢复，摄像机不应逐渐偏离初始位置。
6. 可在配置资产中单独关闭顿帧或震屏，比较前后手感。

本次实现参考：[What Features Influence Impact Feel?](https://arxiv.org/abs/2208.06155) 将顿帧、音效一致性与镜头控制列为可能强烈影响冲击感的因素。当前先实现顿帧和镜头反馈。

计时依据：[Unity Time.timeScale](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Time-timeScale.html)、[Time.unscaledDeltaTime](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Time-unscaledDeltaTime.html)。
