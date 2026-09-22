# MotionCore 架构决策

记录仍有解释价值的取舍和兼容例外，不重复 API 清单或普通修改日志。
这些决策描述当前设计；用户需求改变时可调整，并在相关条目简记原因，无需单独审批流程。

## 1. 玩家与敌人共用命令执行层

输入与 AI 意图都进入 `ICharacterCommandExecutor -> CharacterCommandController`，
共享执行和状态规则；决策差异留在 CharacterBrain 与行为树适配层，避免两套战斗规则。

## 2. CastPriority 与 StaggerLevel 分离

主动动作替换和命中打断是两个问题，分别使用 CastPriority 和 StaggerLevel。
动画退出窗口处理无法仅靠主动优先级表达的切换。

## 3. Hurtbox 保持启用，闪避无敌独立处理

闪避仍参与命中查询，由 Hurtbox 结算层拒绝无敌命中，才能识别真实重叠产生的完美闪避。
身体碰撞单独由 CharacterController 控制，不连带关闭 Hurtbox。

## 4. 卸势独立于闪避

ParryState、ParryStart/End 与 HitParriedEvent 独立表达卸势。
保持输入后的防御结束归 DefenseState，避免两个状态争用动画结束事件。

## 5. 架势破韧与处决资格分离

PostureBreakState 管破韧表现和恢复，ExecutedState 通过 IExecutionTarget 管资格、认领与到期。
处决是高额生命伤害，不等于无条件死亡。

## 6. 强类型同步事件

`struct : IEvent` 通过 IEventBus 分发。scope 区分角色局部状态，全局事件用于跨对象通知；
订阅归消费者生命周期，避免角色互收生命/架势变化。

## 7. 数据和资源保持 Unity 原生作者流

Prefab 与 Unity 资产保存层级、动作、动画和行为树，Luban 保存已接入的角色基础数值及生成记录。
同一字段只保留一个权威；扩展表范围取决于真实消费者，不以全面迁表为目标。

## 8. Widget 和 VFX 的池配置归资源预设

池容量与复用策略分别由 UISystemConfig 与 VfxPreset 保存，调用方传本次播放/绑定参数。
重绑定立即刷新业务值，防止对象池带入前一所有者状态。

## 9. 行为树持续 Tick，反应状态让位行动

IsStaggered 使 Hit、PostureBreak、Executed、Dead 让位到 Wait；状态结束后树自然恢复。
Attack 根据命令真实返回值启动冷却。出生点回位由 IsOutsideHomeRange 与高优先级 Both Abort
分支拥有，阈值不放入控制器，也不散布在动作节点中。树形见 [Architecture](Architecture.md)。

## 10. 死亡世界 UI 保留注册，溶解只更新实例参数

世界生命/架势条死亡时隐藏并保留注册关系，为可逆绑定留出空间。
DeathDissolveFeedback 管游戏时间与碰撞恢复，DeathDissolveView 对现有 Renderer 用
MaterialPropertyBlock 写入 `_DissolveThreshold`，完成后关闭视觉，禁用时恢复参数和原始可见性。
现有材质必须支持该属性；当前实现不克隆材质、不切换 Shader、不依赖材质模板。
这避免反馈层拥有共享材质或角色状态，完整复活仍需独立设计。

## 11. 配置来源与资源来源解耦

IConfigProvider 索引表类型，IAssetProvider 加载 Unity Object，LubanBinaryConfigLoader
通过资源接口读取二进制。组合根构造 Tables，用生成的 GetAllTables 建立索引；
主键与关联查询使用生成表 API，Infrastructure 不依赖业务表类型。
该边界避免重复行包装、逐表手工注册和将资源生命周期混入查询接口。

## 12. 动态角色配置拆分与目录命名

TbCharacter、TbCharacterStat、TbCharacterSpawn 分别表达可复用原型、数值和一次布置，
避免把位置放进原型或从表格重建 Prefab。CharacterSpawner 加载 Prefab，Character.Initialize
统一提交数值。Schema 和 ID 约定只在 [配置设计](Configuration_Architecture.md) 维护。

## 13. 锁定状态与锁定标记分离

TargetLockController 发布携带 Source 的全局 TargetLockChangedEvent；TargetLockMarker
过滤所属玩家并复用 World Widget。查询不创建 UI，怪物不常驻“被当前玩家锁定”的标记，
表现层不重复推断取消、死亡、失活或超距条件。

## 14. 世界 Widget 的局部忽略深度策略

IgnoreDepth 由 Widget 条目显式开启，Prefab 提供 UIWidgetIgnoreDepth 材质。
`ZTest Always` 配合 `ZWrite Off` 让白点穿过 Mesh，同时也穿过墙体和地形；
Canvas sortingOrder 不能代替深度策略。若要保留墙体遮挡，需重新设计可见性规则。
普通世界条保持深度测试，详见 [UI 与基础设施](UI_and_Infrastructure.md)。

## 15. Behavior Designer 在 Unity 6.6 的 OnOpenAsset 兼容修复

既有本地例外：保留包与行为树，对 `Opsive.BehaviorDesigner.Editor.dll` 的
`SubtreeInspector.OnOpenAsset(int, int)` 改为 `OnOpenAsset(EntityId, int)`，
并使用 `EditorUtility.EntityIdToObject`。补丁中的 EntityId 必须编码为值类型，
否则会触发 BadImageFormatException；不据此全局替换 GetInstanceID。

原验证记录为 Unity 6000.6.0f1 编译及进出 Play Mode 无 OnOpenAsset/BadImageFormatException，
并非本次重新验证。升级包或编辑器时复查该补丁；此例外不授权随意修改供应商代码。

## 16. Save 与业务设置分离

ISaveService / JsonSaveService 提供同步整对象保存，默认后端使用 Unity Newtonsoft 包，
支持集合与字典，不启用类型名称反序列化。先写临时文件再替换，缺失返回正常结果，损坏与 IO 错误暴露。
默认值、模型约束、Schema 和迁移归消费者；不额外引入缓存、Changed 或透传服务层。

旧 Settings 与未接入的 PlayerPrefs 后端已移除，未来设置可作为独立存档接入。
当前不保证断电事务、云同步或全平台验证，详见 [Save](../MotionCore/Assets/Scripts/Infrastructure/Save/README.md)。
