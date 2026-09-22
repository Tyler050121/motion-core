# 历史文档同步记录

本文保留旧同步记录供追溯；其中旧类型、方案与验证结论不代表当前实现。
自 2026-09-22 起停止要求逐次续写，维护默认值见 [AGENTS.md](../AGENTS.md)。

## 2026-09-22 文档精简

合并重复的项目契约和基础设施演进盘点；缩短 AGENTS 与技能入口，专题按需读取。
取消普通修改强制同步多文档、ADR 和日志的要求，验证按受影响行为选择。
修正生成路径、启动服务、相机、对象池和死亡溶解说明。只修改文档，未重跑 Unity。

依据：2026-09-22 查阅的 [GPT-6 Astra 模型指导](https://developers.openai.com/api/docs/guides/latest-model)、
[2026-09-11 官方技能与提示审计建议](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra)、
[AGENTS 加载说明](https://learn.chatgpt.com/docs/agent-configuration/agents-md)。
采纳任务相关上下文、精确触发、相称验证和已授权任务的持续执行；这些是本仓库的应用选择，
并非模型性能提升的实测结论。

## 2026-09-19

- 客户端 Luban 生成代码迁移至 `Assets/ScriptsGenerated/Configs/`，同步 Mac/Windows 导出入口并移除旧生成目录；保留脚本 GUID、命名空间和二进制输出路径。
- 验证：执行 Mac 严格导表并比对迁移前后代码、meta 和二进制内容；编辑器导入及 Launch 启动验证待执行。

## 2026-09-17

- 将出生点活动半径从 `EnemyBehaviorController` 移到 `IsOutsideHomeRange` Conditional，明确行为阈值由行为树资产持有，控制器只提供运行时位置事实。
- 将 Goblin 超出出生点活动范围的决策收回行为树：新增 `IsOutsideHomeRange`、`ClearTarget` 和高优先级回位 Sequence，
  使用 `Both` Conditional Abort 打断追击、环绕和攻击。
- 同步行为树节点职责、回位验证项和画布布局约定。

## 2026-09-15

- 锁定切换改为每轮快照：当前目标起步，按环绕角度逐个推进，失效目标跳过，本轮耗尽后取消锁定。
- `TargetLockMarker` 改为通过全局 `TargetLockChangedEvent` 接收锁定变化，并用当前目标引用表示 Widget 是否已注册，去除额外的注册状态标记。
- 忽略深度的 Shader 与 Material 从 `UIWidgetAlwaysOnTop` 改名为 `UIWidgetIgnoreDepth`，与实际深度语义及配置字段保持一致。
- 收敛锁定白点实现：Controller 与 Marker 之间改为直接 C# 事件，移除一对一场景中多余的 scoped EventBus 事件类型。
- `IgnoreDepth` 明确为 Widget 静态配置，只在开启时应用 Prefab 持有的材质；必需配置缺失时立即失败。
- 合并锁定查询过滤与 Widget 创建路径，去除无效状态分支和近似分数比较。

## 2026-09-14

- 为世界 Widget 增加按条目配置的 `IgnoreDepth` 渲染开关；`TargetLockMarker` 使用项目自有 `ZTest Always` UI 材质穿过目标 Mesh，其他世界条仍保持默认深度测试。
- 接入玩家锁定目标白点：`TargetLockController` 发布目标变化，`TargetLockMarker`
  复用 `WorldOverlay` 单对象池跟随目标 `LockPoint`；锁定取消、死亡、失活或超距时同步归还标记。
- 收敛锁定查询：玩家按敌方阵营过滤，屏幕中心判断失败时仍允许最近目标回退，环绕排序补充稳定最终次序。
- 同步当前默认场景设计：`SampleScene` 移除静态 Anbi/Goblin，改由 `CharacterSceneBootstrap` 使用 `3001`、`3002`
  通过 `CharacterSpawner` 动态生成；默认验证入口为 `Launch.unity`。
- 同步 `Character.Initialize(CharacterStat)`、Luban 基础数值字段、`ResourcesAssets` 资源布局和 50 个基础设施
  C# 文件/约 5929 行的现状说明。
- 收口共享 `Characters` 场景父节点下的 P1 角色归属判断：处决、攻击前探、AI 自身引用、卸势和命中特效方向
  均通过角色/组件所有权定位；专项 Play Mode 回归仍待执行。
- 明确 `ResourcesAssetProvider` 当前只读取 `Assets/Resources`，默认 YooAsset 路径与 `ResourcesAssets` 资源收集保持一致。

## 2026-09-13

- 删除 `HealthConfig`、`PostureConfig` 配置对象及 `Character` 上的数值序列化字段；`Health`、`Posture` 初始化入口直接接收 `CharacterStat`，运行时角色数值由配表入口统一提交。
- 清理基础设施的重复依赖空值保护；服务解析、资源 key、VFX 预设和 UI 配置等运行时契约仍在边界处快速失败。
- 收口配置资源字段和 ID 号段约定：单一资源地址使用 `res_key`；角色原型、数值和生成记录分别限制在
  `1001–1999`、`2001–2999` 和 `3001–3999`，并同步重新导出生成代码与二进制。
- 移除临时配置验证源、生成产物及对应文档说明，配置目录只保留第一版角色表。

## 2026-09-12

- 第二轮收口动态角色设计：第一版限定为 `TbCharacter`、`TbCharacterStat`、`TbCharacterSpawn`
  和一个 Gameplay 生成入口，统一主键、引用、资源 key 与位置字段命名。
- 明确第一版不包含动作、AI、阵营、波次和触发关系；角色 Prefab 迁入 `ResourcesAssets`，由根
  Collector 纳入资源包。
- 建立第一版角色 Schema、XLSX、二进制和生成 C#；组合根注册 `CharacterSpawner`，角色支持在
  `Start` 前或启动后应用表格数值，静态 `SampleScene` 暂未切换。
- 将角色资源字段统一为 `res_key`，并按角色原型、数值和生成记录分别采用 `1xxx`、`2xxx`、`3xxx` ID 段。
- 调整死亡溶解的边缘与实体 Alpha，使其在同一消融时间线后段同步减弱，移除独立的后置 Alpha 淡出阶段。
- 收窄 `DeathDissolveFeedback` 生命周期：只响应死亡事件，禁用时统一清理计时器、碰撞排除层和视觉状态，
  不再因回血触发外观重置。
- 新增 `Configuration_Architecture.md`，明确角色原型、数值、动态生成布置和运行时实例的所有权边界。
- 统一配置目录、Schema 类型、源表文件和二进制 key 的大小写约定。
- 整理配表工具目录，移除 `luban/scripts` 层级，统一为 `tools/5.1.0` 和根目录英文平台入口脚本。
- 收敛配表入口文件名为 `MAC_ExportConfig.sh`、`WIN_ExportConfig.bat`、`MAC_CheckConfig.sh` 和 `WIN_CheckConfig.bat`。
- 将可选的只读校验入口移入 `tools/scripts/`，根目录仅保留正式导表入口。
- 明确 `CheckConfig` 与 `ExportConfig` 复用同一套生成阶段校验规则，复杂业务校验不写入平台脚本。
- 将配置注册接入 `ApplicationController` 默认启动链路，在 YooAsset 初始化后直接注册生成的配置表
- 移除手写的角色表包装，主键查询复用 Luban 生成表 API
- 配置注册表改为索引 Luban `Tables` 聚合根，新增生成表不再要求逐表手动注册
- `LubanBinaryConfigLoader` 增加 `IAssetProvider` 构造路径；注册表改用导表生成的类型化表项枚举，移除 `PropertyInfo` 反射
- 将 `RuntimeConfigRegistry` 更名为 `RuntimeConfigProvider`，移除构造期的重复空值检查
- 移除 Luban 生成命名空间的 `Generated` 段和配置注册表的显式 `Seal` 操作
- 将 Luban 生成代码独立到 `Gameplay/Configs/Generated/`，角色现有配置代码继续保留在 `Gameplay/Character/Configs/`
- 同步导表脚本、配置源说明和目录约定，保持生成代码与角色作者数据的职责边界
- 收口配置框架，移除无状态的 Luban 工厂与 Provider 包装，由注册表和二进制读取器承担最小职责
- 移除通用 `IConfigReceiver` 扫描，角色由 `Character` 显式初始化必要组件；Luban loader 收口为 `IAssetProvider` 读取路径

## 2026-09-11

- 接入 Luban 5.1.0 CLI 与 `com.code-philosophy.luban` v1.2.0，建立角色表的源表、Schema、生成脚本、生成代码和二进制读取适配
- 增加 Luban 运行时适配边界，明确生成表与运行时表的映射位置
- 收敛 Luban 读取入口：Infrastructure 只提供二进制 loader 和表集合构造入口，不包含角色表行映射
- 将 Luban 二进制表迁移到 `ResourcesAssets/Configs`，由 YooAsset 资源提供器按需读取；默认应用启动不注册 Luban 配置
- 统一配置框架 XML 注释为职责和生命周期描述

## 2026-09-10

- 整理基础设施契约与文档，保留必要的生命周期兜底
- 增加死亡时世界血条/架势条的隐藏与未来复活兼容规则
- 增加 `DeathDissolveFeedback` 与 `CharacterDeathDissolve` 的责任边界、Prefab 接线和验证项
- 增加死亡表现不注销 Widget、不修改第三方材质的架构决策
- 将死亡溶解从同几何体透明 Overlay 调整为复用原材质参数的 URP Lit 溶解变体，移除反馈层的几何体复制和覆盖过渡

## 2026-09-05

根据当前 `MotionCore/Assets`、`MotionCore/Packages` 和 `MotionCore/ProjectSettings` 全量重整 `docs/`：

- 合并重复的目录规划、命名规划和脚本规范
- 以当前代码重新编写架构、战斗、Prefab、UI/基础设施和验证文档
- 修正 `CharacterBrain`、AI、UI、对象池、YooAsset、事件和时间服务的实际路径与职责
- 补充闪避、卸势、防御、架势破韧、处决和同帧命中合并的真实行为
- 补充敌人行为树在命中、破韧、处决和死亡期间持续 Tick 但让位行动的规则
- 删除引用不存在类型的旧内容，如 `IDamageable`、`EnemyCombatConfig`、`WorldHealthBarManager`
- 删除旧英文 ADR、旧行为树 HTML、协作指南、模板和重复规划稿
- 将仍影响当前实现的决策合并到 `Architecture_Decisions.md`

## 2026-09-19 Settings 重设计

同步用户偏好服务、应用 GameSettings 入口与 JSON 后端职责。参考 Game Framework 标量键值方案，移除整体设置 DTO 和泛型存储 API。验证记录见 Verification.md。

## 2026-09-19：Save 替换 Settings

删除 Settings 与 GameSettings，新增 ISaveService/JsonSaveService，更新组合根、模块 README、ROADMAP 和 API Skill。同步记录 Newtonsoft 直接依赖、文件写入边界与尚未实现的版本迁移范围。历史设置验证不作为现存功能说明。

## 2026-09-19：可选 PlayerPrefs 存档后端

新增 PlayerPrefsSaveService 并补充 Save README。保持 JsonSaveService 为默认后端，不添加后端选择器或新启动配置。

## 2026-09-19：移除 PlayerPrefs 存档后端

删除未接入运行时的 PlayerPrefsSaveService 及对应 README 说明，仅保留 ISaveService 和 JsonSaveService，不修改现有存档数据或启动接线。

## 2026-09-19：修复目录批量加载

修复 YooAsset 空路径双斜杠导致根目录查询为空的问题。批量加载只缓存匹配资源，不匹配句柄立即释放，并复用同步句柄加载逻辑。统一 Resources 路径规范化，更新接口注释和资源契约。

## 2026-09-19：简化共享加载逻辑

移除仅返回句柄的 LoadHandle，将加载、类型判断及句柄缓存或释放集中到 LoadAsset。LoadAll 只负责目录筛选和结果收集，保留全部既有需求，不新增类型或配置。

## 2026-09-19：更新 Infrastructure Roadmap

按当前实现区分 Audio、Save、目录资源加载与 Scene 的能力和边界。明确 Settings 后续基于 Save 接入，PlayerPrefs 后端已移除；将游戏进度、场景流程和异步资源生命周期调整为需求驱动计划，补充验证缺口。仅更新文档，未改变运行时代码。

## 2026-09-19：明确 Save 已完成

修正 Infrastructure Roadmap：Save 当前范围已完成，移除将游戏进度业务接入列为框架待办的表述。下一步为基于现有 Save 的 Settings，业务模型和按需迁移不作为 Save 未完成项。
