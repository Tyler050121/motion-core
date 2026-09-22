# 配置与动态生成设计

## 1. 当前状态

Luban 已包含第一版角色表，用于验证 Schema、源表、生成代码、二进制资源和 `IConfigProvider` 的
读取链路。`CharacterSpawner` 已接入组合根并按生成 ID 创建角色；`SampleScene` 已移除静态 Anbi/Goblin，
由 `CharacterSceneBootstrap` 使用 `3001` 和 `3002` 驱动表加载和实例化。

Anbi、Goblin 仍由现有 Prefab 和 ScriptableObject 提供组件结构、动作、动画及行为树配置。
当前动态路径只覆盖稳定 ID、基础数值和生成位置，不调整战斗动作、AI 或 Prefab 层级。

## 2. 第一版范围

第一版只建立三张表：

```text
TbCharacterSpawn
    └── character_id ──> TbCharacter
                              └── stat_id ──> TbCharacterStat

CharacterSpawner（Gameplay）
    ├── IConfigProvider 查询三张表
    ├── IAssetProvider 加载角色 Prefab
    ├── 按位置和朝向实例化
    └── 在 Instantiate 后、首次逻辑帧前调用 Character.Initialize
```

动态生成表示“从已有角色 Prefab 中选择并实例化”，不表示运行时重新组装角色组件。
当前表未包含动作、连招、技能、命中、AI、阵营、波次和触发器；后续扩展由具体需求决定。

## 3. 表结构

### 3.1 `TbCharacter`

Bean 使用 `CharacterRow`，保存可复用角色原型的最小索引。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | `int` | 主键，稳定角色 ID（1xxx 段） |
| `res_key` | `string` | YooAsset 角色 Prefab 资源地址 |
| `stat_id` | `int` | `TbCharacterStat.id` |

当前没有 `action_set_id`、`ai_id` 或 `faction_id` 消费者。

### 3.2 `TbCharacterStat`

Bean 使用 `CharacterStat`，只保存第一版已有明确消费者的基础数值。

| 字段 | 类型 | 约束 | 当前消费者 |
| --- | --- | --- | --- |
| `id` | `int` | 主键，角色数值 ID（2xxx 段） | `TbCharacter.stat_id` |
| `max_hp` | `float` | `> 0` | `Health` |
| `max_posture` | `float` | `> 0` | `Posture` |
| `posture_regen_delay` | `float` | `>= 0` | `Posture` |
| `posture_regen_rate` | `float` | `>= 0` | `Posture` |
| `move_speed_scale` | `float` | `> 0` | `CharacterRootMotionMotor` |
| `run_turn_back_cooldown` | `float` | `>= 0` | `MoveState` |
| `facing_turn_duration` | `float` | `>= 0` | `CharacterCommandController` |
| `locomotion_turn_duration` | `float` | `>= 0` | `MoveState` / `CharacterCommandController` |
| `combat_turn_duration` | `float` | `>= 0` | `CharacterCommandController` |
| `evade_turn_duration` | `float` | `>= 0` | `CharacterCommandController` |

攻击位移与受击倍率当前仍由战斗作者数据持有，扩展时再确定字段权威。

### 3.3 `TbCharacterSpawn`

Bean 使用 `CharacterSpawn`，保存一次生成所需的最小数据。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | `int` | 主键，生成记录 ID（3xxx 段） |
| `character_id` | `int` | `TbCharacter.id` |
| `pos_x` | `float` | 世界坐标 X |
| `pos_y` | `float` | 世界坐标 Y |
| `pos_z` | `float` | 世界坐标 Z |
| `yaw` | `float` | 绕 Y 轴的角度 |

当前由场景生成入口选择记录 ID；生成组、场景或波次关系尚未实现。

## 4. 字段命名

- Schema 模块、表类型和 Bean 使用 PascalCase：`Character`、`TbCharacterStat`、`CharacterRow`、`CharacterSpawn`。
- Schema 字段、XLSX 列名、源表文件和资源 key 使用小写 snake_case。
- 每张表的主键统一为 `id`；引用字段使用 `xxx_id`；单一资源地址字段统一使用 `res_key`。
- 使用项目内已明确的常用缩写，例如 `hp`、`id`、`res`；不缩写 `character`、`posture` 等领域名称。
- 当前 ID 约定使用角色原型 `1xxx`、角色数值 `2xxx` 和生成记录 `3xxx` 段；Schema 将各段限制为
  `1001–1999`、`2001–2999` 和 `3001–3999`。
  超出当前号段时，应先扩展号段约定和 Schema，再继续录入数据。
- 字段名表达单位或语义，不重复表名。生成位置使用 `pos_x`，不使用
  `character_spawn_position_x`；每秒恢复值使用 `posture_regen_rate`，不使用含糊的 `regen`。

配置领域目录保持 PascalCase，例如 `Data/Character` 和 `Defines/Character.xml`；生成 C# 位于
`Assets/ScriptsGenerated/Configs`，不手工修改生成文件。

## 5. 运行时边界

`CharacterSpawner` 属于 Gameplay，直接消费生成表关联，没有额外的行包装层。
它只完成以下流程：

1. 用生成表的 `Get(id)` 查询 `CharacterSpawn`。
2. 使用生成行的 `CharacterId_Ref`、`StatId_Ref` 取得关联行。
3. 用 `res_key` 从 `IAssetProvider` 加载 Prefab。
4. 实例化角色并提交 `CharacterStat`。

业务入口只需要选择生成记录：

```csharp
CharacterSpawner spawner = ServiceLocator.Resolve<CharacterSpawner>();
Character character = spawner.Spawn(spawnId, sceneRoot);
```

`CharacterSpawner` 已由 `ApplicationController` 在配置表加载完成后注册；业务代码不需要自行构造
Luban `Tables` 或按表注册类型。

`Character.Initialize(CharacterStat)` 将定义和同一份数值提交给 CharacterCommandController、
Health、Posture 与 CharacterRootMotionMotor。初始化在 Instantiate 返回后、首次 Start/Update 前完成；
Awake/OnEnable 不能假定已经收到表值。组件不独立查询表或扫描通用配置接收器。

`SampleScene` 选择 `3001`（Anbi）与 `3002`（Goblin）。每个实例仍以自身 Character 为所有权边界，
共享的 Characters 父节点仅负责组合；生成记录 ID 不作为持久化实例身份。

## 6. 资源边界

角色 Prefab 和配置二进制保存在 `Assets/ResourcesAssets`，由根 Collector 统一收集。
不再为 `Assets/Prefabs/Characters` 配置独立 Collector，`res_key` 只承担 YooAsset 资源寻址职责；
资源类型由表契约和消费端确定。Launch 当前使用 YooAsset；`ResourcesAssetProvider` 仍存在，但
`Resources.Load` 只搜索 `Assets/Resources`，不能直接读取当前 `ResourcesAssets` 布局。

当前 Collector 使用 `AddressByFileName`，角色地址可使用 `Anbi`、`Goblin`。修改资源或收集规则时检查
地址唯一性；运行时找不到资源或 Prefab 缺少 `Character` 组件时报告错误，不提供默认角色。

## 7. 导出与验证

路径相对仓库根：源表 `../config/`，工具 `../tools/`。使用正式入口
`MAC_ExportConfig.sh` 或 `WIN_ExportConfig.bat`，只读检查位于 `../tools/scripts/`。
生成 C# 到 `MotionCore/Assets/ScriptsGenerated/Configs/`，二进制到
`MotionCore/Assets/ResourcesAssets/Configs/`。

Schema 校验主键、引用和数值范围；Unity 侧确认资源地址唯一且存在、Prefab 根包含 Character。
受影响的配置验证选择：导出/校验 -> YooAsset 加载 -> 3001/3002 生成 -> 变更字段效果与初始化时序。
具体观察项和已有证据边界见 [Verification](Verification.md)。

## 8. 设计依据

三张表分别对应原型、数值和一次布置，生命周期不同；资源与作者结构仍由 Unity 管理。
实现入口：CharacterSpawner、Character.Initialize、生成的 Tables 与 AssetBundleCollectorSetting。
扩展动作、AI 或关卡关系时再明确消费者和字段权威，不预设全面迁移路线。
