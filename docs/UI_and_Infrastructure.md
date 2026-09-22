# UI 与运行时基础设施

服务由 `ApplicationController` 装配；启动顺序见 [Architecture](Architecture.md)。
API 示例见 [Infrastructure API](../.agents/skills/infrastructure-usage/references/api.md)，本页只说明所有权和生命周期。

## 服务边界

| 服务 | 所有权与使用边界 |
| --- | --- |
| `ServiceLocator` | 应用级注册；重复注册覆盖，未注册 Resolve 返回 null，Unregister 只移除同一实例 |
| `IAssetProvider` | 加载 Unity Object，调用方负责实例化；YooAsset 实现持有缓存句柄并在 Dispose 释放 |
| `IConfigProvider` | 按表类型查询；组合根构造 Tables 和索引，LubanBinaryConfigLoader 使用资源接口读取二进制 |
| `IEventBus` | 强类型同步事件；跨对象通知用全局，单个 Health/Posture/Hurtbox 通知用对象 scope |
| `ITimerService` | 组合根按 deltaTime 推进；消费者持有 TimerHandle 并在生命周期结束时移除 |
| `IGameTimeService` | 集中写 timeScale；按 unscaledDeltaTime 推进慢动作，暂停优先，Reset 恢复为 1 |
| `ICursorService` | 启动、更新和焦点变化由组合根转发 |
| `ISceneNavigator` | 加载场景并转发事件；当前未实现完整加载界面、取消和恢复流程 |
| `IAudioService` | 播放、跟随、Mixer 音量与 AudioSource 复用；循环声音由消费者 Stop |
| `ISaveService` | 同步读写完整数据；业务默认值、保存时机与版本迁移归消费者 |

Infrastructure 不依赖生成的业务表类型。配置查询、Unity 资源生命周期和 Gameplay 行映射分别持有职责。
当前没有通用异步资源句柄 API；出现实际消费者时再设计生命周期。

## UI 装配与作用域

```text
ApplicationController -> UIRuntimeBootstrap.Boot -> UIRuntime
                                              ├── UIManager
                                              └── UIElementManager
```

`UISystemConfig` 定义 Root、CanvasScaler、层、Scope 和元素/Widget 资源及池配置。
`UIManager` 管 Scope、打开/关闭与 ESC 返回栈；`UIElementManager` 管实例、过渡和池。
`SampleScene` 使用 Gameplay Scope 并自动打开 CombatHudPanel，具体资源地址和容量直接查配置资产。

屏幕 UI 使用 Screen Space 层。世界 Widget 创建到 `WorldOverlay` World Space Canvas，
源实现 `IWorldWidget`，提供 WidgetId、Anchor、Offset、Bind 和 Unbind。
`LateUpdate` 处理创建、绑定及位置/朝向刷新；不是屏幕坐标投影。

## 绑定、锁定与死亡 UI

- `CombatHudPanel` 监听玩家生成，绑定当前 Health/Posture 和处决提示。
- 世界生命/架势条按组件 scope 订阅事件。首次绑定和池复用使用 Immediate API 初始化，避免继承上一角色的显示值。
- `TargetLockMarker` 监听全局 `TargetLockChangedEvent`，只接受 Source 等于自身 GameObject 的事件。
  当前目标引用就是注册状态来源；切换目标只更新 LockPoint，取消锁定时注销并归还 Widget。
- 世界生命/架势条在死亡时隐藏已绑定实例，保留源注册关系；死亡期间架势事件不会重新显示。
  生命恢复可刷新 UI，但不代表完整复活或状态机复位已实现。

`IgnoreDepth` 是 Widget 静态配置。开启时要求 Prefab 根的 `UIWidgetRenderSettings` 提供
`UIWidgetIgnoreDepth` 材质，创建/租借时应用。当前只用于锁定白点，普通世界条保留深度测试。
它会同时穿过角色、墙体和地形，不能表达“只穿过目标”。

死亡本体由 `DeathDissolveFeedback` / `DeathDissolveView` 管理，见 [Combat](Combat.md)。
UI 不拥有角色死亡状态或材质生命周期。

## 资源与池

`LoadAll<T>` 查询目录及子目录，空路径表示资源根。两个 Provider 统一路径分隔符与首尾斜杠。
YooAsset 批量查询先加载主资源再判断类型，仅缓存匹配句柄，不匹配立即 Release；
单项与批量加载复用缓存。混合目录存在同步探测成本，优先查询职责明确的小目录。
Release 表示释放引用，不保证底层资源立即卸载。

`PrefabPool<T>` 支持预热、按需扩容、压力缓存与空闲衰减，使用租借集合检查归还所有权。
外部或重复归还的有效实例会报错，销毁/null 实例归还为空操作。
消费者负责在租借/归还或 Bind/Unbind 中重置状态。详见源码旁的
[Pooling README](../MotionCore/Assets/Scripts/Infrastructure/Pooling/README.md)。

`VfxPreset` 保存资源 key、复用模式、释放延迟和容量；`IVfxService.Play` 接收本次位姿、速度与跟随参数。
`AttackVfxDefinition` / `HitVfxDefinition` 保存作者数据，VfxService 持有 PooledVfx 池。

## Audio 与 Save

[Audio README](../MotionCore/Assets/Scripts/Infrastructure/Audio/README.md) 说明 AudioPreset、AudioBus、
2D/3D/跟随播放及停止责任。Mixer 参数缺失是配置错误；Audio 当前不负责偏好持久化。

[Save README](../MotionCore/Assets/Scripts/Infrastructure/Save/README.md) 说明 JSON 文件契约。
默认 `JsonSaveService` 目录为 `persistentDataPath/Saves`；先序列化和写临时文件，再替换正式文件。
缺失文件与损坏数据分开处理；退出只注销服务，不自动保存。
Settings 尚未重新接入，旧实验偏好文件不自动迁移。

## 演进与验证

不预设 UI 拆层、工厂、时间域或资源句柄的强制迁移阶段；实际复杂度出现时再选择设计。
受影响的池、UI 竞态、World Widget、时间和启动路径按 [Verification](Verification.md) 验证。
