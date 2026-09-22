# MotionCore 当前架构

MotionCore 是可调试、可替换的 Unity 3D 动作战斗框架原型。当前有 Anbi 玩家、Goblin 敌人、
近战战斗、锁定、行为树、UI 和运行时服务。联网战斗、通用投射物、复杂技能编辑器与回放尚未实现；
这些是能力描述，不是后续开发禁区。

## 启动与服务

Unity 项目根是 `MotionCore/`。`Launch.unity` 的 `GameRoot` 挂载
`ApplicationController` 与 `UIRuntimeBootstrap`，默认随后加载 `SampleScene`。

```text
ApplicationController
├── Awake：注册 Cursor / Timer / GameTime / EventBus / Assets / Audio / Save / Vfx / Scene
├── Start：等待资源就绪 -> 构造配置表 -> 注册 CharacterSpawner -> Boot UI -> 加载启动场景
├── Update：驱动 Cursor / Timer / GameTime / Audio
└── OnDestroy：关闭 UI、注销服务并释放持有的资源
```

默认 `YooAssetProvider` 使用 `ResourcesAssets` 收集内容；编辑器模式与包名由 Launch 配置，
Player 当前使用 Offline。`ResourcesAssetProvider` 仅适用于 `Assets/Resources`。
`ServiceLocator` 是应用级装配入口，不能替代角色 Prefab 接线；能够显式注入的消费者使用窄接口。
服务生命周期见 [UI 与基础设施](UI_and_Infrastructure.md)。

## 角色执行链

```text
CharacterBrain（玩家输入、相机相对移动、锁定）
EnemyBehaviorController（敌人事实、目标记忆、命令适配）
        -> ICharacterCommandExecutor
        -> CharacterCommandController（输入缓冲、命令、切换请求）
        -> CharacterState FSM（执行和生命周期）
        -> Animancer（动画与事件）
        -> MeleeHitbox -> Hurtbox -> Health / Posture -> 反馈与 UI
```

`Character` 持有角色上下文、定义与必要组件。`CharacterSceneBootstrap` 选择生成记录，
`CharacterSpawner` 读取 `TbCharacterSpawn -> TbCharacter -> TbCharacterStat`，加载并实例化 Prefab，
立即调用 `Character.Initialize`，在首次逻辑帧前统一提交数值。场景 `Characters` 只是组合父节点，
不承担角色所有权。表和初始化边界见 [配置设计](Configuration_Architecture.md)。

`CastPriority` 处理主动动作替换，`StaggerLevel` 处理受击打断；动画事件打开退出与命中窗口。
状态切换细节见 [战斗系统](Combat.md) 和 `CharacterStateRules`，不要在输入或 AI 重复执行规则。

## 相机与锁定

`CameraController` 实现 `ICameraService`，封装视口转换、平面方向以及 Cinemachine 相机控制。
`LockOnTarget` 维护可查询目标，`LockOnTargetQuery` 过滤与选择，`TargetLockController` 管生命周期。

首次锁定优先屏幕中心候选，否则选择最近目标。切换使用一次锁定时建立的环绕快照，跳过失效目标，
本轮耗尽后取消；死亡、失活和超距也会取消。Controller 发布全局 `TargetLockChangedEvent`，
`TargetLockMarker` 按 Source GameObject 过滤并驱动 World Widget；查询层不创建 UI。
具体材质、池化与绑定见 [UI 与基础设施](UI_and_Infrastructure.md)。

## AI 决策

Behavior Designer 适配位于 `Gameplay/AI/BehaviorDesigner/`。节点通过
`EnemyBehaviorController` 读取事实、下发命令，行为阈值由树节点资产持有。

```text
Repeater
└── Selector
    ├── IsStaggered -> Wait
    ├── IsOutsideHomeRange -> ClearTarget -> MoveTo(Home)
    ├── DetectTarget -> 交战 Selector
    │                  ├── 范围内 + AttackReady + Face + Attack
    │                  ├── 范围内 + Orbit
    │                  └── MoveTo 追击
    └── PickPatrolPoint -> MoveTo -> Wait
```

- 反应状态 `Hit`、`PostureBreak`、`Executed`、`Dead` 让位到 Wait，树仍 Tick，结束后恢复决策。
- `IsOutsideHomeRange` 比较出生点平面距离；节点默认半径 8 米，0 关闭约束。
  回位 Sequence 在交战之前，用 `Both` Conditional Abort 中断交战，再清目标与回位。
- `Attack` 只有在状态机接受命令后开始冷却，拒绝重入返回失败。

Prefab 接线见 [Prefab 契约](Prefab_Contract.md)，验证重点见 [Verification](Verification.md)。
