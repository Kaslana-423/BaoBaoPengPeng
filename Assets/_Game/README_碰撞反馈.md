# 碰撞反馈：十字闪光、角色闪白、顿帧与震屏

碰撞接触点生成白色中心、金色外沿的十字闪光，并让碰撞角色短暂闪白；较强碰撞加入短顿帧和衰减震屏。已有场景直接进入 Play 即可，碰撞伤害系统会自动添加 ImpactFeedback，无需重新搭建场景或绑定摄像机。

只有不同阵营角色碰撞时才生成十字闪光并让双方闪白；撞墙、同阵营角色互撞均不闪光、不闪白。只有接触开始时检查强度，持续贴住不会重复触发，同一次碰撞对只生成一次特效。强碰撞顿帧与震屏仍适用于角色或墙壁碰撞。碰撞越强，光芒尺寸、顿帧时长和震屏幅度越大。视觉反馈不施加额外冲量，也不改变攻击力、质量、阻尼或行动方伤害规则。

## 调整位置

在 Project 面板选择 **Assets/_Game/Resources/ImpactFeedbackSettings.asset**。这份配置会自动加载，退出 Play 后调整可长期保存。

| 参数 | 默认值 | 作用 |
| --- | --- | --- |
| Minimum Impact Speed | 2 | 震屏最小触发相对速度 |
| Full Impact Speed | 8 | 达到该速度时使用最大震幅、光芒尺寸和顿帧时长 |
| Enable Hit Stop | 开 | 是否启用短顿帧 |
| Minimum Hit Stop Speed | 4 | 只在相对速度达到这个阈值后触发卡帧 |
| Hit Stop Seconds | 0.045 | 刚达到强碰撞阈值时停顿 45 毫秒 |
| Maximum Hit Stop Seconds | 0.075 | 最大强度停顿 75 毫秒；代码硬上限 0.12 秒 |
| Enable Spark / Enable White Flash | 开 | 分别开关十字闪光和角色闪白 |
| Minimum Visual Speed | 0.35 | 十字闪光、闪白的最小触发相对速度 |
| Spark Lifetime | 0.18 | 光芒从出现、扩张到消退的真实秒数 |
| Minimum / Maximum Spark Size | 0.8 / 1.55 | 光芒尺寸倍率，随碰撞强度变化 |
| Spark Color | 暖金色 | 十字光芒外沿、射线和柔光颜色，中心保持白色 |
| White Flash Seconds | 0.09 | 角色闪白持续约 90 毫秒 |
| White Flash Intensity | 1 | 闪白覆盖强度，调低可保留更多角色底色 |
| Enable Shake | 开 | 是否启用震屏 |
| Shake Seconds | 0.14 | 震屏持续时间，随时间快速衰减 |
| Minimum Shake Distance | 0.03 | 刚达到阈值时的震幅，单位为世界单位 |
| Maximum Shake Distance | 0.12 | 最大震幅 |
| Shake Frequency | 35 | 震动频率参数 |
| Feedback Cooldown | 0.12 | 顿帧、震屏各自的最短间隔；不限制伤害、十字闪光和闪白 |

想更重一点，可以把 Hit Stop Seconds 调到 **0.06**、Maximum Shake Distance 调到 **0.16**。想更利落，可以把顿帧降到 **0.03**。这些是本项目的初始调试值，最终以实际 Play 手感为准。

墙角连撞仍按每次真正分离再接触扣血。Feedback Cooldown 不会限制伤害；它只是防止密集碰撞让画面连续冻结。顿帧使用真实时间恢复，不修改刚体速度；震屏只添加摄像机临时偏移，结束后移除。组件停用、销毁或窗口失焦时也会清理反馈。

十字闪光与闪白使用不受时停影响的时间。闪光采用对象池，最多同时显示 24 个；光芒网格复用，不需要图片贴图。角色闪白通过独立 SpriteRenderer 和透明轮廓着色器覆盖，不改角色原颜色/材质，也不修改属性 UI。角色复活时立即清除上一条生命的闪白。比分暂停前会释放碰撞时停、清理特效，再由比分系统持有暂停，避免被顿帧计时器误恢复。

新资产位于 `_Game/Resources/FX/`：`PF_CollisionSpark.prefab`、`M_CollisionSpark.mat`、`M_HitWhiteFlash.mat`。运行时自动加载，不需要手动拖进场景。`Tools > Tuanzi > Create Impact Visual Assets` 可补建缺失资产，不覆盖已有资产。

## 文件

- `Scripts/Data/ImpactFeedbackSettings.cs`：配置类型。
- `Resources/ImpactFeedbackSettings.asset`：实际默认参数。
- `Scripts/Combat/ImpactFeedback.cs`：真实时间顿帧、镜头震动和清理恢复。
- `Scripts/Combat/CollisionDamageSystem.cs`：自动初始化反馈、验证当前是否正在行动。
- `Scripts/Units/UnitCollisionReporter.cs`：在角色/墙面碰撞开始时上报强度，同一角色碰撞对只上报一次。
- `Scripts/Combat/ImpactVisuals.cs` / `CollisionSparkView.cs`：接触点十字光芒、柔光、射线与对象池。
- `Scripts/Units/UnitHitFlash.cs` / `Shaders/HitWhiteFlash.shader`：角色轮廓闪白与复活清理。

## Play 验收

1. 打开现有 SC_Battle，进入 Play，用弹力团子满蓄力正面撞敌方，观察短暂的撞击停顿和快速震屏。
2. 小力度拖动碰撞，低于触发速度时不应顿帧或震屏。
3. 强力撞墙和撞队友仍可顿帧、震屏，但不会生成十字闪光或角色闪白；友方与墙面依然不直接扣血。
4. 测试墙角多次反弹，伤害可以连续触发，反馈不会每个物理帧都触发。
5. 在 Play 中查看 GameSystems/PrototypeSession 上自动生成的 Impact Feedback 组件。碰撞结束后游戏速度恢复，摄像机不应逐渐偏离初始位置。
6. 可在配置资产中单独关闭顿帧或震屏，比较前后手感。

本次实现参考：[What Features Influence Impact Feel?](https://arxiv.org/abs/2208.06155) 将顿帧、音效一致性与镜头控制列为可能强烈影响冲击感的因素。当前先实现顿帧和镜头反馈。

计时依据：[Unity Time.timeScale](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Time-timeScale.html)、[Time.unscaledDeltaTime](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Time-unscaledDeltaTime.html)。
