# MotionCore 文档

按任务查阅，无需顺序通读。项目是 Unity 3D 动作战斗框架原型；文档保留模块职责、接入方式和不易从代码看出的取舍，具体实现以代码、资产和配置为准。

Unity 项目在 `MotionCore/`。编辑器版本查 [ProjectVersion.txt](../MotionCore/ProjectSettings/ProjectVersion.txt)，
依赖查 [Packages](../MotionCore/Packages/manifest.json)。从 `Launch.unity` 进入 `SampleScene` 运行默认闭环。

| 需要了解 | 查阅 |
| --- | --- |
| 启动、角色分层、AI | [Architecture](Architecture.md) |
| 战斗状态与动画事件 | [Combat](Combat.md) |
| 角色、UI 和场景接线 | [Prefab_Contract](Prefab_Contract.md) |
| Luban 表、生成与初始化 | [Configuration_Architecture](Configuration_Architecture.md) |
| UI、资源、事件、时间及服务生命周期 | [UI_and_Infrastructure](UI_and_Infrastructure.md) |
| 目录、命名与作者数据归属 | [Asset_And_Code_Conventions](Asset_And_Code_Conventions.md) |
| 按改动选择验证路径 | [Verification](Verification.md) |
| 非显然的设计理由、包兼容补丁 | [Architecture_Decisions](Architecture_Decisions.md) |
| Infrastructure API 调用示例 | [API Reference](../.agents/skills/infrastructure-usage/references/api.md) |
| Audio / Save / Pooling 的完整接入契约 | [Audio](../MotionCore/Assets/Scripts/Infrastructure/Audio/README.md) · [Save](../MotionCore/Assets/Scripts/Infrastructure/Save/README.md) · [Pooling](../MotionCore/Assets/Scripts/Infrastructure/Pooling/README.md) |

协作与维护默认值只放在 [AGENTS.md](../AGENTS.md)。专题文档描述当前设计，不冻结后续架构；
仅在受影响说明失真时更新对应位置。无需为普通修复维护多份文档、评审报告或变更日志。
[历史同步记录](Document_Changelog.md) 仅供追溯，不要求续写，也不作为当前能力或待办。
