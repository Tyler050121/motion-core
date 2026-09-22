# MotionCore Prefab 契约

## 角色通用结构

当前 Anbi 和 Goblin 都以角色根为中心，已使用的责任节点如下：

```text
CharacterRoot
├── Brain&Actions
├── CameraTarget
├── HitboxRoot
│   └── Anchor_Weapon       Anbi 当前使用
├── HurtboxRoot
├── ExecutionAnchor
└── Visual / 模型层级       由导入模型提供
```

该图是职责概览，具体层级以当前 Prefab、嵌套实例和覆盖为准。新增节点由实际组件职责决定。

## Character 根组件

`Character` 必须正确引用：

- `AnimancerComponent`
- `FacingRoot`
- 角色挂点数组
- `CharacterDefinition`
- `Health` 和 `Posture` 组件

运行时 `Character.Initialize(CharacterStat)` 在实例化后、首次逻辑帧前将：

- `CharacterDefinition` 和 `CharacterStat` 传给 `CharacterCommandController`
- `CharacterStat` 传给 `Health`
- `CharacterStat` 传给 `Posture`
- `CharacterStat` 传给 `CharacterRootMotionMotor`

由 `CharacterSpawner` 创建的动态实例会立即提交 `CharacterStat`，为上述运行时数值消费者提供参数；
`Character` 不保存生命或架势配置字段。手工放置角色若没有经过同一初始化入口，不属于有效运行时接线。

角色 Prefab 位于 `Assets/ResourcesAssets/Characters`，由 `ResourcesAssets` 根 Collector 收集；场景实例仍可
通过 Prefab GUID 保持引用。

`CharacterController` 位于角色根，由 `Character` 直接获取；`CharacterRootMotionMotor` 可以位于子层级。
`SampleScene` 的 `Characters` 只是动态实例的场景组合父节点，不是任何角色的 ownership root。依赖角色归属的
逻辑不得用该节点的 `transform.root` 代替角色根、`Character`、`LockOnTarget` 或 `Health`。

## Anbi

Anbi 是玩家角色，`Brain&Actions` 至少包含：

- `CharacterBrain`
- `CharacterCommandController`
- `MoveState`
- `EvadeState`
- `ParryState`
- `DefenseState`
- `AttackState`
- `HitState`
- `PostureBreakState`
- `ExecutionState`
- `DeadState`
- `TargetLockController`
- `TargetLockMarker`

角色根或相关责任节点还需要：

- `Health`
- `Posture`
- `LockOnTarget`
- `Hurtbox`
- `HitReceiver`
- `CharacterRootMotionMotor`
- `PerfectDodgeFeedback`
- `PostureBreakFeedback`

Anbi 的 `LockOnTarget` 阵营是 `Player`。
`CharacterBrain` 负责读取 `InputActions`，并通过 `TargetLockController` 处理锁定；
`TargetLockMarker` 位于同一 `Brain&Actions` 节点，监听锁定变化并驱动 `WorldOverlay` 中的白点。
Anbi 的 `TargetLockController` 当前将 `Enemy` 作为可锁定阵营。

## Goblin

Goblin 是敌人角色，`Brain&Actions` 至少包含：

- `EnemyBehaviorController`
- Behavior Designer 行为树运行组件
- `CharacterCommandController`
- `MoveState`
- `AttackState`
- `HitState`
- `PostureBreakState`
- `DeadState`

当前 Goblin 的 `EvadeState`、`ParryState`、`DefenseState` 和 `ExecutionState` 引用为空，
因此不能把 Goblin 描述为拥有完整玩家防御链路。

Goblin 的 `LockOnTarget` 阵营是 `Enemy`。
行为树资源是：

```text
Assets/BehaviorTrees/Enemies/Goblin/Goblin_BehaviorTree.asset
```

树节点与回位/反应让位规则见 [Architecture](Architecture.md#ai-决策)，本页仅描述 Prefab 接线。

## 必须保持的引用关系

```text
Character
├── CharacterDefinition
├── CharacterRootMotionMotor -> Character / Animator / CharacterController
└── CharacterCommandController
    ├── Character
    ├── MoveState
    ├── EvadeState        玩家角色需要
    ├── ParryState        玩家角色需要
    ├── DefenseState      玩家角色需要
    ├── AttackState
    ├── HitState
    ├── PostureBreakState
    ├── ExecutionState    可处决攻击者需要
    └── DeadState

AttackState -> MeleeHitbox
MeleeHitbox -> CharacterAnchor / HitProfile
Hurtbox -> Health / Posture / LockOnTarget / EventBus
OverheadHealthBar -> LockOnTarget / Health
OverheadPostureBar -> LockOnTarget / Posture / Health
TargetLockMarker -> TargetLockChangedEvent / IUIService
DeathDissolveFeedback -> Health / Character / DeathDissolveView
DeathDissolveView -> 现有子层级 Renderer / 支持 _DissolveThreshold 的材质
```

死亡溶解组件放在稳定的角色/视觉责任节点；View 查找子层级 MeshRenderer 与 SkinnedMeshRenderer。
现有材质需要支持 `_DissolveThreshold`，运行时只用 MaterialPropertyBlock 更新参数，不再引用溶解模板或克隆材质。

不能只看脚本文件名判断接线是否正确。Prefab Variant 的 `m_RemovedComponents`、嵌套实例和场景覆盖
都可能改变实际运行结构。

## UI Prefab

### 屏幕 HUD

```text
CombatHudPanel
├── PlayerStatusGroup
│   └── PlayerVitalsBar
│       ├── ShieldFrameMask
│       │   └── ShieldFrameFill
│       ├── ShieldFrameTrack
│       ├── HealthBarTrack
│       ├── HealthBarFill
│       └── CapRingGroup
│           ├── LeftCapRing
│           └── RightCapRing
└── ExecutionPrompt
    └── Text (TMP)
```

以上是当前 HUD Prefab 实际结构。`CombatHudPanel` 直接引用 `HealthBar`、`PostureBar` 和处决提示根节点。

### 可复用条

```text
HealthBar
├── Track
├── Fill
├── DamageFill
└── HealFill

PostureBar
├── Track
└── Fill
```

绑定新角色或池对象重新租借时必须调用：

- `HealthBar.SetHealthImmediate`
- `PostureBar.SetPostureImmediate`

普通实时变化才使用带表现延迟/平滑的接口。

### 世界 Widget

```text
ResourcesAssets/UI/Widgets/OverheadHealthBar.prefab
ResourcesAssets/UI/Widgets/OverheadPostureBar.prefab
```

世界 Widget 由 `UIElementManager` 创建到 `WorldOverlay` World Space Canvas，
通过 `IWorldWidget.Anchor + Offset` 更新位置，并面向渲染相机。
`OverheadHealthBar` 和 `OverheadPostureBar` 在 `Bind` 时从实例根获取对应 Bar。
`TargetLockMarker.prefab` 为单根 `RectTransform + CanvasRenderer + UnityEngine.UI.Image`，使用现有 UI Atlas
圆形 Sprite，并在根节点挂载 `UIWidgetRenderSettings` 及项目自有 `UIWidgetIgnoreDepth` 材质；它不挂在怪物 Prefab 上，
而由玩家的 `TargetLockMarker` 源对象动态指向怪物 `LockPoint`。Prefab 的渲染设置组件只负责保存和应用材质，
是否启用由 `UISystemConfig.UIWidgetEntry.IgnoreDepth` 控制。

## 场景职责

- `Launch.unity`：应用入口、全局服务和 UI 启动器
- `SampleScene.unity`：当前战斗演示场景，包含环境、相机、`Characters` 组合节点和
  `CharacterSceneBootstrap`；Anbi/Goblin 由生成记录动态创建
- `UIScene.unity`：UI 系统结构验证场景，不等同于运行时自动生成的 UI 根

可复用角色、相机、UI 和 VFX 结构放在 Prefab；场景只保存组合、实例位置和必要覆盖。
默认验证应从 `Launch.unity` 进入；`CharacterSceneBootstrap` 依赖组合根已经注册 `CharacterSpawner`。
