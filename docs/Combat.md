# 战斗系统

## 核心闭环

```text
输入或 AI 意图
-> CharacterCommandController
-> CharacterState FSM
-> Animancer 动画
-> Hit / HitStart / HitEnd 事件
-> MeleeHitbox
-> Hurtbox
-> Health / Posture
-> HitEvent / 受击状态 / VFX / UI
```

## 角色状态

当前顶层状态：

| 状态 | 类型 | 作用 |
| --- | --- | --- |
| `IdleState` | Locomotion | 默认待机 |
| `MoveState` | Locomotion | 起步、走跑循环、收招、急转身 |
| `EvadeState` | Locomotion | 闪避位移和无敌 |
| `AttackState` | Action | 普攻、后继攻击、完美变体和命中 |
| `DefenseState` | Action | 防御循环、减伤和结束 |
| `ParryState` | Action | 卸势窗口和成功后的防御/攻击路由 |
| `ExecutionState` | Action | 选择目标、认领并播放处决动作 |
| `HitState` | Reaction | 普通受击或击退受击 |
| `PostureBreakState` | Reaction | 破韧起始、等待和恢复 |
| `ExecutedState` | Reaction | 被处决窗口和配对反应 |
| `DeadState` | Reaction | 死亡终态 |

`Idle` 不作为退出窗口。动画完成后通过 `ForceSetDefaultState` 回到默认状态。

## 优先级和僵直

`CastPriority` 与 `StaggerLevel` 是两个独立序列：

```text
CastPriority:
BasicAttack < HeavyAttack < Skill < Ultimate < Defense < Evade < Parry < Execution

StaggerLevel:
LightAttack < ChargedAttack < MartialSkill < Block < Toughness
             < PostureBreak < Parry < GuardBreak < Execution
```

施法优先级解决“主动动作能否自动替换当前动作”；僵直等级解决“命中能否进入受击状态”。
二者不能合并成一个枚举。

当前 `CharacterCommandController.ReceiveHit` 规则：

- 破韧期间保持 `PostureBreakState`，只替换破韧受击表现
- 命中僵直等级为 `None` 或低于当前状态等级时忽略
- 同一帧多次命中由 `HitReceiver` 合并，取最高僵直等级和最大击退力度
- 生命归零通过 `HealthChangedEvent` 延迟进入 `DeadState`

生命归零同时驱动世界生命/架势条隐藏，以及独立的死亡表现。
`DeathDissolveFeedback` 监听 scoped HealthChangedEvent，默认等待 3 秒、再按游戏时间溶解 3 秒；
`DeathDissolveView` 从现有 Renderer 读取并更新 MaterialPropertyBlock 的 `_DissolveThreshold`，
完成后关闭视觉。现有材质需支持该属性，不在运行时克隆材质或切换 Shader。
溶解开始时由 Character 排除角色实体碰撞，反馈禁用时移除自己的排除层并恢复视觉参数。
反馈不拥有角色状态，也不通过回血自动复活；完整复活流程尚未实现。

敌人反应状态期间的决策让位见 [Architecture 的 AI 决策](Architecture.md#ai-决策)。

## 攻击数据

```text
AttackDefinition
├── StateType
├── CastPriority
├── StaggerLevel
├── Steps[]
│   ├── Track
│   ├── EndStep
│   ├── PerfectVariant
│   ├── ComboGraceSeconds
│   └── ComboGraceStartType
└── ComboFollowUp / ComboFollowUpStepIndex

AttackAnimationTrack
├── Animation
└── Hits[]
    ├── HitProfile
    ├── CharacterAnchor
    ├── LocalOffset
    └── AttackVfxDefinition
```

`AttackState` 复用一个状态实例执行多个 `AttackDefinition`。
输入在 `CanAttack`、`CanCancel` 和连段宽限窗口内推进；收招轨道只负责动作结束，不再承担攻击判定。
行为树的 `Attack` 节点只有在命令控制器确认状态机接受攻击后才开始攻击冷却和等待；
攻击状态内的重入请求同样返回 `TryResetState` 的真实结果，避免把被拒绝的请求误判为已出手。

Anbi 当前有：

- `Anbi_Attack_Normal`
- `Anbi_DodgeCounter`
- `Anbi_Execution_Definition`
- 普攻 1 至 4 段、完美变体和收招 Track

Goblin 当前拥有自己的普通攻击定义、命中配置和 Track，但角色定义和状态接线仍需以 Goblin Prefab 为准。
Goblin 普攻的状态僵直阈值配置为 `Toughness`：普通轻击仍会造成生命/架势伤害，但不会直接打断
Goblin 当前普攻；架势归零仍通过 `PostureBreakState` 打断。

## 命中检测和结算

`MeleeHitbox` 使用 `Physics.OverlapSphereNonAlloc` 查询配置层中的 Hurtbox。

- `Hit`：瞬时采样一次
- `Open`：打开持续窗口，每个窗口对同一 `Hurtbox` 只结算一次
- `Close` / `CloseAll`：关闭持续窗口
- 攻击者自身按 `Hurtbox.Health == attacker Health` 过滤
- 同阵营跳过
- `HitStart` 被无敌拒绝的命中仍会占用窗口，避免窗口结束时补中

这里的 `Health` 自身命中过滤、`LockOnTarget` 自身锁定过滤，以及处决、卸势反击、攻击前探、AI 自身查找
和表现点方向的角色归属判断，均不依赖 `transform.root`，适用于多个角色共享 `Characters` 场景组合父节点。
运行时专项回归仍需在共享父节点场景下确认这些角色/组件所有权边界。

`Hurtbox` 在自身状态内结算：

1. 已死亡：忽略
2. 正在卸势：发布 scoped `HitParriedEvent`，不发布 `HitEvent`
3. 正在闪避无敌：发布 scoped `HitAvoidedEvent`，不发布 `HitEvent`
4. 正常命中：应用生命和架势倍率，发布 scoped `HitEvent`

`HitProfile` 当前保存：

- 生命伤害
- 架势伤害
- 命中半径
- 目标层
- 僵直等级
- 击退力度
- 命中 VFX 列表

`Hurtbox` 直接持有并调用 `Health` 和 `Posture`。

## 闪避和完美闪避

`EvadeState` 通过动画事件控制无敌：

- `InvulnerableStart/End`：Hurtbox 无敌

Hurtbox 保持启用，因为命中实际重叠是完美闪避成立的必要条件。
第一次被无敌拒绝的命中会：

1. 发布 `HitAvoidedEvent`
2. 让 `EvadeState` 关闭本次监听
3. 获得一次闪避反击资格
4. 打开攻击退出窗口
5. 播放 `PerfectDodgeFeedback`

普通轻击输入消费该资格并请求角色配置中的 `DodgeCounterAttack`，不会自动反击。
Anbi 的反击定义可从指定段索引继续接普通攻击。

## 卸势和防御

`ParryState` 用 `ParryStart` 和 `ParryEnd` 打开/关闭卸势窗口。
命中在窗口内会进入 `Hurtbox` 的 parry 分支，发布 `HitParriedEvent`，不伤害防守者。
卸势成功不会触发完美闪避反击。

卸势输入保持时：

```text
ParryState -> DefenseState Loop
```

松开输入时：

- 没有移动输入：直接回默认状态
- 有移动输入：等待当前阶段结束，统一播放 `DefenseState` 的 End，再进入移动

`DefenseState` 当前默认：

- 生命伤害倍率 `0.8`
- 架势伤害倍率 `1.1`

离开防御时恢复两个倍率为 `1`。

## 架势和处决

`Posture` 是独立于生命的资源。架势归零时发布 `PostureChangedEvent(IsNewlyBroken = true)`。
`PostureBreakState` 负责：

- 播放破韧起始动画
- 开启 `ExecutedState` 持有的可处决窗口
- 根据 `m_LockDuringWaiting` 决定是否锁定等待
- 播放破韧期间受击表现
- 窗口到期后恢复架势

可处决资格由 `ExecutedState : IExecutionTarget` 持有，不要求攻击者和目标硬编码成某两个角色。
`ExecutionState.TryPrepare` 从 `LockOnTarget.Targets` 中选择：

- 不是自己
- 目标可用且有 `Character`
- 目标当前允许被处决
- 有 `ExecutionAnchor`
- 在最大距离和最大朝向角内
- 按朝向权重和距离权重评分最低者

进入 `ExecutionState` 后，攻击者先认领目标，再由处决动画事件完成高额 `Health` 伤害。
处决不是无条件死亡；只有生命归零才进入 `DeadState`。

## 动画事件约定

通用事件：

```text
CanCancel
CanInterrupt
CanAttack
CanEvade
Hit
HitStart
HitEnd
InvulnerableStart
InvulnerableEnd
ParryStart
ParryEnd
CharacterCollisionOff
CharacterCollisionOn
```

事件名与 `Assets/ScriptableObjects/Events/` 下的资源名对应。
`CharacterCollisionOff/On` 当前仅保留事件契约，运行时尚未绑定。
`CharacterState.PlayWithEvents` 负责统一绑定和旧动画过滤，子状态只能通过 `BindEvent` 扩展事件。
