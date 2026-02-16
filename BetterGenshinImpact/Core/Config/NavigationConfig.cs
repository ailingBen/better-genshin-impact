using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.Core.Config;

[Serializable]
public partial class NavigationConfig : ObservableObject
{
    /// <summary>
    /// 是否启用导航系统
    /// </summary>
    [ObservableProperty]
    private bool _enabled = true;

    /// <summary>
    /// GroundingDINO 置信度阈值
    /// </summary>
    [ObservableProperty]
    private float _confidenceThreshold = 0.5f;

    /// <summary>
    /// 栅格地图宽度
    /// </summary>
    [ObservableProperty]
    private int _gridWidth = 80;

    /// <summary>
    /// 栅格地图高度
    /// </summary>
    [ObservableProperty]
    private int _gridHeight = 60;

    /// <summary>
    /// 目标引力场强度
    /// </summary>
    [ObservableProperty]
    private float _goalAttractionStrength = 1.2f;

    /// <summary>
    /// 障碍物斥力场强度
    /// </summary>
    [ObservableProperty]
    private float _obstacleRepulsionStrength = 6.0f;

    /// <summary>
    /// 障碍物影响半径
    /// </summary>
    [ObservableProperty]
    private float _obstacleInfluenceRadius = 12.0f;

    /// <summary>
    /// 探索场强度
    /// </summary>
    [ObservableProperty]
    private float _explorationStrength = 0.08f;

    /// <summary>
    /// 导航规划间隔(ms)
    /// </summary>
    [ObservableProperty]
    private int _navigationInterval = 80;

    /// <summary>
    /// 是否显示导航调试信息
    /// </summary>
    [ObservableProperty]
    private bool _showDebugInfo = false;

    /// <summary>
    /// 力场计算的窗口半径
    /// </summary>
    [ObservableProperty]
    private int _forceCalculationWindowRadius = 8;

    /// <summary>
    /// 路径平滑系数
    /// </summary>
    [ObservableProperty]
    private float _pathSmoothingFactor = 0.7f;

    /// <summary>
    /// 最大障碍力阈值
    /// </summary>
    [ObservableProperty]
    private float _maxObstacleForce = 2.0f;

    /// <summary>
    /// 目标接近阈值
    /// </summary>
    [ObservableProperty]
    private float _goalProximityThreshold = 0.05f;

    /// <summary>
    /// 障碍安全距离
    /// </summary>
    [ObservableProperty]
    private float _obstacleSafetyDistance = 0.1f;

    /// <summary>
    /// 速度阻尼系数
    /// </summary>
    [ObservableProperty]
    private float _velocityDampingFactor = 0.95f;
}

