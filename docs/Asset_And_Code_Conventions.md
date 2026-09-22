# 目录与命名

沿用现有职责目录；新目录由实际文件和消费者驱动。

## 资源与代码位置

所有 `Assets/` 路径相对 Unity 根 `MotionCore/`。

| 位置 | 内容 |
| --- | --- |
| `Assets/Art/` | 原始、导入美术 |
| `Assets/BehaviorTrees/` | 项目行为树 |
| `Assets/Prefabs/` | 非资源加载的可复用场景组合 |
| `Assets/ResourcesAssets/` | YooAsset 运行时资源，按角色、UI、VFX、Audio、配置等职责组织 |
| `Assets/Scenes/` | 场景组合 |
| `Assets/ScriptableObjects/` | 角色、动作、动画事件和表现作者数据 |
| `Assets/Settings/` | 项目与插件设置 |
| `Assets/Scripts/ApplicationLifecycle/` | 组合根与启动 |
| `Assets/Scripts/Gameplay/` | 角色、战斗、AI、相机、输入、锁定、UI |
| `Assets/Scripts/Infrastructure/` | 资源、配置、事件、计时、池、UI、VFX、Audio、Save、场景与光标 |
| `Assets/ScriptsGenerated/Configs/` | Luban 生成代码 |
| `Assets/ThirdParty/`、`Assets/Samples/`、`Packages/` | 第三方与样例边界 |

角色配置适配在 `Gameplay/Character/Configs/`；状态在 `Gameplay/Character/States/` 的
`Actions`、`Locomotion`、`Reactions` 下；攻击类型在 `Gameplay/Combat/Attack/`；
Behavior Designer 节点在 `Gameplay/AI/BehaviorDesigner/`。不要在文档另维护完整代码文件树。

角色独占作者资产放在 `ScriptableObjects/Character/<Name>/`，攻击资源按
`Actions/Attack/Definitions`、`HitProfiles`、`Tracks` 组织。通用事件在
`ScriptableObjects/Events/`，UI/VFX 配置在对应职责目录。

## 命名与代码

- C# 文件与类型同名，一个文件一个 MonoBehaviour；使用可搜索的完整名称。
- 私有序列化字段沿用 `m_`；容易误接的字段用 Tooltip，非显然的公共契约与生命周期用简短注释说明。
- Prefab 和层级按职责命名；角色专属资产带角色前缀，例如 `Anbi_Attack_Normal`。
- 必需依赖在所有权边界报告缺失；正常不可执行返回明确结果，避免用兜底资源掩盖接线错误。
- 插件调用留在适配边界。扩展优先写项目代码，生成文件通过生成入口更新。

配置字段、ID 号段和源表路径统一见 [配置设计](Configuration_Architecture.md)；
第三方兼容例外见 [架构决策](Architecture_Decisions.md)。
