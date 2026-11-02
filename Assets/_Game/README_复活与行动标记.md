# 出生点复活、大轮与未使用标记

直接打开现有 **Assets/Scenes/SC_Battle.unity** 并进入 Play，无需重建场景或手动绑定组件。

## 当前规则

- 角色被打到 0 血后，短暂退出物理接触，然后在本局最初出生位置满血复活。位置按场景实际坐标记录，不硬编码左右位置。
- 复活会清除旧速度，恢复碰撞与外观；沿用同一个角色对象和同一份行动记录。
- **未使用角色死亡：复活后仍未使用。已使用角色死亡：复活后仍已使用。**
- 敌我双方的六名角色都各主动发射一次，并完成最后一次物理结算后，才结束一大轮。
- 新大轮清空六名角色的使用记录，玩家先手。死亡或暂时正在复活不会让角色退出本轮名单，也不会提前进入下一大轮。
- 继续保留双方交替行动、三档 AI、行动方单向伤害、墙角连撞和强碰撞反馈。
- 已移除全灭胜负判定。本阶段不计分、不判输赢，对局会继续进行。

## 旋转箭头圆环

我方尚未使用的角色会显示金色双箭头圆环，环绕角色与底部攻击/血量 UI，并缓慢旋转。

- 我方成功发射的瞬间，所有我方圆环立即隐藏；取消拖拽不隐藏。
- 物理结算、敌方思考和敌方行动期间均不显示圆环。
- 再次轮到我方选择时，仅显示本大轮尚未使用角色的圆环。
- 被打死或复活不改变使用记录，也不会在非我方选择阶段意外显示圆环。
- 新大轮开始时，我方三名角色重新显示圆环。
- 敌方不显示此标记。圆环不拦截鼠标，不参与物理碰撞。

圆环在 UnitStatsUI 下自动生成，名称为 **UnusedActionRing**。可在运行时的 ActionRingGraphic 组件中调整 Rotation Speed（默认 70°/秒）与 Thickness（默认 3.2 UI 像素）。需要长期改变默认样式时，可修改共用 Prefab 中的该对象，或调整 ActionRingGraphic 默认值。

## 实现与接口

| 文件（相对 Assets/_Game） | 职责 |
| --- | --- |
| Scripts/Units/UnitActor.cs | 记录出生坐标，提供 RespawnAtSpawn，重置血量、刚体和外观；保留 Defeated，新增 Respawned 事件。 |
| Scripts/Combat/RespawnController.cs | 监听击倒，等待旧接触退出后复活；不修改行动记录。 |
| Scripts/Combat/CollisionDamageSystem.cs | 复活前清除旧生命的接触去重记录，让新生命能正常参与后续碰撞。 |
| Scripts/Core/TurnManager.cs | 使用记录只在成功发射或新大轮时变化；六名角色全部用完才满足 AllHaveActed。 |
| Scripts/Core/GameFlowController.cs | 不再按存活人数判断胜负或缩短大轮，连接我方行动标记。 |
| Scripts/Core/PrototypeSession.cs | 自动配置复活系统，待复活处理完成后再允许结束物理结算。 |
| Scripts/UI/UnitStatsUI.cs | 订阅行动资格变化，控制圆环显示。 |
| Scripts/UI/ActionRingGraphic.cs | 绘制双箭头弧线并旋转。 |

可读取 `unit.SpawnPosition`、`session.Flow.Turns.HasActed(unit)` 和 `session.Flow.Turns.AllHaveActed`。`TurnManager.EligibilityChanged` 可供后续 UI 订阅。`UnitActor.Defeated` 和 `Respawned` 可供以后计分或表现系统使用，本次没有实现分数。

旧的 `BattleEnded`、`Winner` 和按全灭进入 Victory/Defeat 的状态已移除，后续需要计分结束规则时再添加对应接口。难度调整接口 `session.SetAIDifficulty(int)` 保持可用。

## 验收步骤

1. 进入 Play，确认我方三个金色箭头圆环旋转，敌方没有圆环。
2. 发射一名我方角色，其圆环立即消失，其他两名保留。
3. 让尚未使用的角色被打死，确认其在最初出生点满血复活，仍有圆环，并可在我方可操作时使用。
4. 让本轮已经用过的角色被打死，确认复活后圆环仍隐藏，本轮不能再次使用。
5. 双方各用完三个角色之后，确认进入下一大轮，我方三名圆环都重新出现。
6. 连续击倒或同时击倒多名角色，确认仍回到各自出生点，行动标记不变，对局不会全灭结束。

退出 Play 后可用 **Tools > Tuanzi > Validate Turns And AI** 验证大轮、行动标记、复活和三档 AI；用 **Validate Unit Combat Stats** 验证复活后的碰撞接触处理和血量 UI。

为了不在碰撞回调内部反复拆装刚体，复活队列会留出一个物理步清理旧接触，再重置位置；默认固定步长下为数十毫秒。复活定位属于显式重置，只在刚体模拟关闭时同步 Transform 与 Rigidbody2D，正常运动仍完全交给物理系统。
