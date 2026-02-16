# 导航系统集成指南

## 📋 概述

本导航系统已成功集成到 Better Genshin Impact 项目中，包含以下核心模块：

1. **感知层** - GroundingDINO 零样本目标检测
2. **地图表示** - 屏幕空间栅格地图
3. **规划层** - 复合势场法导航
4. **执行层** - 自动导航任务

---

## 📁 模块文件结构

```
BetterGenshinImpact/
├── Core/Navigation/
│   ├── Model/
│   │   ├── DetectedObject.cs          # 检测对象模型
│   │   └── OccupancyGrid.cs           # 栅格地图模型
│   ├── GroundingDinoPredictor.cs      # GroundingDINO 预测器
│   ├── PotentialFieldNavigator.cs     # 复合势场法导航器
│   ├── NavigationManager.cs            # 导航管理器
│   ├── MODEL_SETUP.md                  # 模型设置指南
│   └── INTEGRATION_GUIDE.md           # 本文档
└── GameTask/AutoNavigate/
    └── AutoNavigateTask.cs             # 自动导航游戏任务
```

---

## 🔧 已完成的修改

### 1. 新增文件

- ✅ `Core/Navigation/Model/DetectedObject.cs`
- ✅ `Core/Navigation/Model/OccupancyGrid.cs`
- ✅ `Core/Navigation/GroundingDinoPredictor.cs`
- ✅ `Core/Navigation/PotentialFieldNavigator.cs`
- ✅ `Core/Navigation/NavigationManager.cs`
- ✅ `GameTask/AutoNavigate/AutoNavigateTask.cs`
- ✅ `Core/Navigation/MODEL_SETUP.md`
- ✅ `Core/Navigation/INTEGRATION_GUIDE.md`

### 2. 修改文件

- ✅ `Core/Recognition/ONNX/BgiOnnxModel.cs` - 添加 GroundingDINO 模型注册
- ✅ `Core/Script/Dependence/Genshin.cs` - 解决命名空间冲突

---

## 📦 模型安装

### 方法一：使用预导出模型（推荐）

1. 从以下平台寻找 GroundingDINO ONNX 模型：
   - GitHub Releases
   - Hugging Face: https://huggingface.co/models?search=groundingdino
   - Modelscope: https://www.modelscope.cn/models

2. 将模型文件放置到：
   ```
   BetterGenshinImpact/Assets/Model/Navigation/groundingdino.onnx
   ```

### 方法二：自行导出模型

详细步骤请参考 `MODEL_SETUP.md` 文档。

---

## 🚀 快速开始

### 1. 构建项目

项目已成功编译，可以直接运行：

```bash
dotnet build BetterGenshinImpact.sln
```

### 2. 使用导航系统

```csharp
using BetterGenshinImpact.Core.Navigation;
using Microsoft.Extensions.Logging;

// 1. 创建导航管理器
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<NavigationManager>();
var navManager = new NavigationManager(logger);

// 2. 初始化
navManager.Initialize();

// 3. 获取导航器
var navigator = navManager.GetNavigator();

// 4. 设置参数（可选）
navigator.GoalWeight = 1.0f;
navigator.ObstacleWeight = 2.0f;
navigator.ExploreWeight = 0.3f;
```

### 3. 使用自动导航任务

```csharp
using BetterGenshinImpact.GameTask.AutoNavigate;
using OpenCvSharp;

// 创建任务
var navTask = new AutoNavigateTask(cancellationToken);

// 设置目标位置（屏幕坐标 0.0-1.0）
var goalPos = new Point2f(0.8f, 0.5f);

// 可选：设置 GroundingDINO 预测器
navTask.SetGroundingDinoPredictor(dinoPredictor);

// 开始导航
await navTask.StartAsync(goalPos);
```

---

## 🎛️ 可调参数

### PotentialFieldNavigator 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `GoalWeight` | 1.0f | 目标引力场权重 |
| `ObstacleWeight` | 2.0f | 障碍物斥力场权重 |
| `ExploreWeight` | 0.3f | 探索场权重 |
| `ObstacleRadius` | 50 | 障碍物影响半径（像素） |
| `WindowRadius` | 10 | 势场计算窗口半径 |

### AutoNavigateTask 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `DetectionInterval` | 3 | 检测间隔（帧数） |
| `GridWidth` | 80 | 栅格地图宽度 |
| `GridHeight` | 60 | 栅格地图高度 |
| `ObstaclePrompt` | "enemy, wall, obstacle" | 障碍物检测提示词 |
| `GoalPrompt` | "door, exit, treasure" | 目标检测提示词 |

---

## 📊 架构说明

### 感知层 (GroundingDinoPredictor)

- 输入：游戏截图 (ImageRegion)
- 输出：检测到的对象列表 (List&lt;DetectedObject&gt;)
- 支持文本提示词零样本检测

### 地图表示 (OccupancyGrid)

- 80×60 或自定义分辨率栅格
- 单元格类型：自由、障碍物、目标、玩家
- 将检测框投影到栅格

### 规划层 (PotentialFieldNavigator)

- 复合势场法：目标引力 + 障碍物斥力 + 探索场
- 计算合力向量作为移动方向
- 访问过的单元格记录

### 执行层 (AutoNavigateTask)

- 整合所有模块
- 隔帧检测优化性能
- 键盘输入模拟 (WASD)

---

## ⚠️ 注意事项

1. **模型文件**：GroundingDINO 模型文件较大（约 2GB），请确保有足够磁盘空间
2. **性能优化**：建议使用 GPU 加速（DirectML 或 CUDA）
3. **首次加载**：首次加载模型可能需要较长时间
4. **参数调优**：根据实际游戏场景调整势场权重参数

---

## 🔮 后续优化方向

1. 添加深度估计模型 (Depth Anything V2)
2. 实现 A* 全局路径规划
3. 添加 UI 配置界面
4. 优化 GroundingDINO 文本嵌入处理
5. 添加更多避障策略

---

## 📚 参考文档

- [GroundingDINO 论文](https://arxiv.org/pdf/2303.05499.pdf)
- [GroundingDINO GitHub](https://github.com/IDEA-Research/GroundingDINO)
- [AAAI 2025 复合势场法论文](https://arxiv.org/abs/2412.xxxxx)
