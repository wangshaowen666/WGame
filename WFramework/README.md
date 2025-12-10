# WFramework

Unity框架设计，包含常用设计模式与工具类，适用于游戏开发中的基础架构搭建。

## 功能特性

- **日志系统**：支持彩色输出与日志等级控制
- **对象池**：提供高效的类对象与Unity对象回收机制
- **有限状态机**：支持状态切换与数据传递
- **协程管理**：统一协程调度接口
- **资源加载**：封装Addressables资源加载逻辑
- **热更新**：支持资源热更新流程
- **性能分析**：提供时间性能分析工具

## 目录结构

```
├── Base            // 基础功能模块
├── Module          // 核心框架模块
├── Profiler        // 性能分析工具
├── Test            // 功能测试用例
└── Util            // 工具类扩展
```

## 快速开始

1. 将本框架导入Unity项目Assets目录
2. 在Hierarchy视图创建空对象并挂载`HotUpdater`组件启动热更新流程
3. 使用`ResMgr.Instance.LoadSync<T>("key")`进行资源加载
4. 通过`Procedure.Instance.RunProcedure<YourProcedure>()`启动流程管理

## 使用示例

```csharp
// 定义状态机
public class GameStartState : FsmState 
{
    public override void OnEnter()
    {
        Log.Info("进入游戏启动状态");
    }
}

// 创建状态机
var fsm = Fsm.Create(new List<FsmState> { 
    new GameStartState(), 
    new MainMenuState() 
});
fsm.ChangeState<GameStartState>();
```

## 贡献指南

1. Fork仓库
2. 创建功能分支
3. 实现功能并添加单元测试
4. 提交PR并填写完整变更说明
5. 等待代码评审

## 许可协议

本项目采用MIT开源协议，详细条款请参阅LICENSE文件。