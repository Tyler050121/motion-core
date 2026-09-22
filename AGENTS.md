# MotionCore

Unity 3D 动作战斗框架原型。保持输入/AI、命令、状态、动画、命中、结算与表现的职责清楚，优先完成当前需求。

## 工作方式

- 用户当前任务决定范围；本文和技能是项目默认指导。按任务读取相关代码和资料，无需全量阅读文档。
- 在已授权范围内自行处理常规实现选择，完成修改、相关验证和修复；只有缺失信息会实质改变结果时才提问。
- 当前代码、资产和配置是实现依据。文档中的现状、历史和计划不是新增功能的授权，也不限制用户提出的新设计。
- 保留无关工作区和暂存内容。未经明确要求，不运行 git add、commit、reset、checkout 或其他改变暂存区的操作。
- 用简洁中文说明结果、验证和实际未解决项。

## 项目入口

- Unity 根目录是 `MotionCore/`；版本查 `MotionCore/ProjectSettings/ProjectVersion.txt`，包查 `MotionCore/Packages/`。
- 运行从 `MotionCore/Assets/Scenes/Launch.unity` 进入 `SampleScene`；后者依赖启动链注册服务，不能作为独立启动入口。
- 业务代码在 `MotionCore/Assets/Scripts/`；运行时资源在 `MotionCore/Assets/ResourcesAssets/`，默认使用 YooAsset。
  `ResourcesAssetProvider` 只读取真实的 `Assets/Resources` 布局。
- Luban 源表和工具相对仓库根位于 `../config/`、`../tools/`。正式导出入口为
  `MAC_ExportConfig.sh` / `WIN_ExportConfig.bat`；生成代码在 `MotionCore/Assets/ScriptsGenerated/Configs/`，不手改。

## 关键边界

- 玩家输入与敌人 AI 共用 `ICharacterCommandExecutor -> CharacterCommandController -> CharacterState -> Animancer`。
  行为树负责决策，控制器适配事实与命令，状态负责执行；插件细节留在对应适配边界。
- `CharacterSpawner` 实例化后通过 `Character.Initialize(CharacterStat)` 统一初始化。
  场景的 `Characters` 是组合父节点，角色所有权通过 `Character` 等组件定位，不用 `transform.root` 猜测。
- 同一字段保持单一权威：C# 管行为，Prefab 管接线，Unity 资产管作者资源，配置表管已接入的基础数值。
  `IConfigProvider` 查询配置，`IAssetProvider` 加载资源。
- 沿用现有模块和窄接口；新增抽象、依赖或数据层应解决当前需求。必需接线在所属边界暴露错误，
  可选表现与正常的“不可执行”使用显式结果。
- 修改 Prefab、场景或序列化资产时检查受影响的层级、引用、变体和覆盖；移动资产保留 GUID。
  第三方与生成内容不是日常编辑入口；已有兼容补丁见架构决策。

## 验证与文档

- 验证与改动相称：纯文档检查内容和链接；代码检查 Unity 导入/编译与受影响路径；
  序列化或生命周期变更检查加载及相关 Play Mode 行为。使用 Unity 工具时确认目标项目是本仓库的 `MotionCore/`。
- 静态检查和 dotnet build 不能证明 Unity 运行行为。无法运行时明确已做检查及待验证项；
  相关检查通过后，无新证据不反复扩大全量回归。
- 只有变更使现有说明失真，或产生代码难以表达的长期决策时，更新最相关的一处文档。
  日常修复无需配套报告、ADR、日志或多文档同步；有跨模块取舍时简记原因即可，不把文档作为实施前置审批。

## 按需参考

| 任务 | 入口 |
| --- | --- |
| 启动、模块边界、AI | [Architecture](docs/Architecture.md) |
| 状态、动画事件、命中 | [Combat](docs/Combat.md) |
| Prefab 接线 | [Prefab](docs/Prefab_Contract.md) |
| 配置与导表 | [Configuration](docs/Configuration_Architecture.md) |
| UI、资源及服务生命周期 | [UI / Infrastructure](docs/UI_and_Infrastructure.md) |
| 目录与命名 | [Conventions](docs/Asset_And_Code_Conventions.md) |
| 受影响路径的验收 | [Verification](docs/Verification.md) |
| 设计取舍与兼容补丁 | [Decisions](docs/Architecture_Decisions.md) |
