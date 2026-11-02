# 辅助瞄准：虚线与一次反弹

直接打开现有 SC_Battle 场景进入 Play，按住我方角色向后拖动即可，无需重新搭建场景或绑定引用。

- 辅助线改为浅色圆头虚线，随鼠标方向实时更新。
- 蓄力越大，预览越长。满蓄力时总路径默认最多 **6 个世界单位**。
- 初始段和反弹段共享总长度，不会每段各延伸 6 个单位。
- 使用角色实际 CircleCollider2D 的世界半径扫掠，能检测擦边碰撞。
- 碰到队友、敌人、墙壁等有碰撞体的障碍，按接触面的法线显示一次反弹方向。
- 反弹后再碰到障碍就停止，不会显示第二次反弹后的路径。
- 忽略角色自身、Trigger，以及物理碰撞设置中忽略的碰撞层或碰撞对。
- 松手发射、右键或 Esc 取消时隐藏。

## Inspector 调整

选中场景根对象 **AimPreview**，在 **Aim Preview** 组件中调整：

| 参数 | 默认值 | 作用 |
| --- | --- | --- |
| Max Preview Length | 6 | 满蓄力的总长度上限，包含反弹段 |
| Obstacle Layers | Everything | 哪些碰撞层参与反弹预测 |
| Path Color | 浅白色 | 虚线颜色 |
| Line Width | 0.09 | 虚线宽度 |
| Dash Spacing | 0.3 | 虚线重复间距，越大越稀疏 |

PlayerInputController 的 Max Drag Distance（默认 2）继续控制蓄满所需的拖拽距离，与辅助线长度分别调整。

## 文件

- `Scripts/Input/AimPreview.cs`：虚线绘制、预览总长度、显示及资源清理。
- `Scripts/Input/AimTrajectory.cs`：角色半径扫掠、第一次反射、第二处障碍截断。
- `Scripts/Input/PlayerInputController.cs`：把所选角色碰撞体交给预览计算。
- `Tests/Editor/AimPreviewChecks.cs`：真实 Unity 2D 查询验证；菜单 `Tools > Tuanzi > Validate Aim Preview`。

## Play 验收

1. 朝空地拖动，逐渐拉满后辅助线不再变长。
2. 对准队友或敌方，再稍微偏转角度，观察反弹方向随圆形表面的接触位置变化。
3. 对准墙面斜射，观察折线反弹；初始段越长，剩余反弹段越短。
4. 让反弹段遇到另一名角色或另一面墙，辅助线应在第二次接触处结束。
5. 松手、Esc 或右键后，辅助线应消失。

这是基于当前障碍位置的方向辅助。实际撞击可移动角色时，运动方向还会受质量、弹性和目标受撞后的移动影响；辅助线不模拟完整的多刚体运动。

实现 API 参考：[Unity 2022.3 LineRenderer.textureScale](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/LineRenderer-textureScale.html)、[Unity 2022.3 Physics2D 官方接口源码](https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Modules/Physics2D/ScriptBindings/Physics2D.bindings.cs)。
