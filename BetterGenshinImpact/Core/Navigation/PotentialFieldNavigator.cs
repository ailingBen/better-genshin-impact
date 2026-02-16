using System;
using System.Collections.Generic;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Navigation.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Navigation;

public class PotentialFieldNavigator
{
    private readonly NavigationConfig _config;
    private readonly HashSet<(int, int)> _visitedCells = new();
    private Point2f _previousForce = new Point2f(0, 0);

    public PotentialFieldNavigator(NavigationConfig config)
    {
        _config = config;
    }

    public Point2f ComputeNavigationForce(Point2f playerPos, Point2f goalPos, OccupancyGrid grid)
    {
        var totalForce = new Point2f(0, 0);
        int playerGridX = (int)(playerPos.X * grid.Width);
        int playerGridY = (int)(playerPos.Y * grid.Height);

        // 计算到目标的距离
        float distanceToGoal = (float)Math.Sqrt(Math.Pow(goalPos.X - playerPos.X, 2) + Math.Pow(goalPos.Y - playerPos.Y, 2));
        
        // 如果已经接近目标，返回零力
        if (distanceToGoal < _config.GoalProximityThreshold)
        {
            return new Point2f(0, 0);
        }

        // 使用配置的窗口半径
        int windowRadius = _config.ForceCalculationWindowRadius;
        for (int dx = -windowRadius; dx <= windowRadius; dx++)
        {
            for (int dy = -windowRadius; dy <= windowRadius; dy++)
            {
                int x = playerGridX + dx;
                int y = playerGridY + dy;

                if (!grid.IsInBounds(x, y))
                    continue;

                var cellPos = new Point2f((float)x / grid.Width, (float)y / grid.Height);
                var cellForce = ComputeCellForce(cellPos, playerPos, goalPos, grid, x, y);
                totalForce += cellForce;
            }
        }

        // 限制最大障碍力
        float obstacleForceMagnitude = (float)Math.Sqrt(totalForce.X * totalForce.X + totalForce.Y * totalForce.Y);
        if (obstacleForceMagnitude > _config.MaxObstacleForce)
        {
            float scale = _config.MaxObstacleForce / obstacleForceMagnitude;
            totalForce = new Point2f(totalForce.X * scale, totalForce.Y * scale);
        }

        // 路径平滑 - 与之前的力加权平均
        totalForce = new Point2f(
            totalForce.X * (1 - _config.PathSmoothingFactor) + _previousForce.X * _config.PathSmoothingFactor,
            totalForce.Y * (1 - _config.PathSmoothingFactor) + _previousForce.Y * _config.PathSmoothingFactor
        );

        // 归一化
        float magnitude = (float)Math.Sqrt(totalForce.X * totalForce.X + totalForce.Y * totalForce.Y);
        if (magnitude > 0.001f)
        {
            totalForce = new Point2f(totalForce.X / magnitude, totalForce.Y / magnitude);
        }

        // 速度阻尼
        totalForce = new Point2f(
            totalForce.X * _config.VelocityDampingFactor,
            totalForce.Y * _config.VelocityDampingFactor
        );

        _previousForce = totalForce;
        _visitedCells.Add((playerGridX, playerGridY));

        return totalForce;
    }

    private Point2f ComputeCellForce(Point2f cellPos, Point2f playerPos, Point2f goalPos, OccupancyGrid grid, int gridX, int gridY)
    {
        var totalForce = new Point2f(0, 0);

        var goalForce = ComputeGoalForce(cellPos, goalPos);
        totalForce += goalForce * _config.GoalAttractionStrength;

        var obstacleForce = ComputeObstacleForce(cellPos, playerPos, grid, gridX, gridY);
        totalForce += obstacleForce * _config.ObstacleRepulsionStrength;

        var exploreForce = ComputeExploreForce(cellPos, gridX, gridY);
        totalForce += exploreForce * _config.ExplorationStrength;

        return totalForce;
    }

    private Point2f ComputeGoalForce(Point2f pos, Point2f goal)
    {
        var dx = goal.X - pos.X;
        var dy = goal.Y - pos.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.001f)
            return new Point2f(0, 0);

        var normalizedX = dx / distance;
        var normalizedY = dy / distance;
        // 改进的引力计算 - 距离越近，引力增长越缓慢
        var magnitude = 1.0f / (distance + 0.2f);

        return new Point2f((float)(normalizedX * magnitude), (float)(normalizedY * magnitude));
    }

    private Point2f ComputeObstacleForce(Point2f cellPos, Point2f playerPos, OccupancyGrid grid, int gridX, int gridY)
    {
        var force = new Point2f(0, 0);
        var cellType = grid.GetCell(gridX, gridY);

        if (cellType == OccupancyGrid.CellType.Obstacle)
        {
            var dx = playerPos.X - cellPos.X;
            var dy = playerPos.Y - cellPos.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            // 考虑安全距离
            var effectiveDistance = Math.Max(0, distance - _config.ObstacleSafetyDistance);
            
            if (effectiveDistance > 0.001f && effectiveDistance < (float)_config.ObstacleInfluenceRadius / Math.Max(grid.Width, grid.Height))
            {
                var normalizedX = dx / distance;
                var normalizedY = dy / distance;
                // 改进的斥力计算 - 距离越近，斥力增长越快
                var magnitude = 1.0f / ((effectiveDistance * effectiveDistance) + 0.01f);
                force = new Point2f((float)(normalizedX * magnitude), (float)(normalizedY * magnitude));
            }
        }

        return force;
    }

    private Point2f ComputeExploreForce(Point2f cellPos, int gridX, int gridY)
    {
        if (!_visitedCells.Contains((gridX, gridY)))
        {
            return new Point2f(_config.ExplorationStrength, _config.ExplorationStrength);
        }
        return new Point2f(0, 0);
    }

    public void UpdateOccupancyGrid(OccupancyGrid grid, List<DetectedObject> detections, Point2f goalPosScreen)
    {
        grid.Clear();

        foreach (var detection in detections)
        {
            int startX = (int)(detection.BBox.X * grid.Width);
            int startY = (int)(detection.BBox.Y * grid.Height);
            int endX = (int)((detection.BBox.X + detection.BBox.Width) * grid.Width);
            int endY = (int)((detection.BBox.Y + detection.BBox.Height) * grid.Height);

            // 扩展障碍物边界，增加安全性
            int safetyMargin = Math.Max(1, (int)(grid.Width * 0.02));
            startX = Math.Max(0, startX - safetyMargin);
            startY = Math.Max(0, startY - safetyMargin);
            endX = Math.Min(grid.Width - 1, endX + safetyMargin);
            endY = Math.Min(grid.Height - 1, endY + safetyMargin);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (detection.Label.ToLower().Contains("enemy") ||
                        detection.Label.ToLower().Contains("wall") ||
                        detection.Label.ToLower().Contains("obstacle") ||
                        detection.Label.ToLower().Contains("barrier") ||
                        detection.Label.ToLower().Contains("block"))
                    {
                        grid.SetCell(x, y, OccupancyGrid.CellType.Obstacle);
                    }
                }
            }
        }

        // 标记目标区域
        int goalGridX = (int)(goalPosScreen.X * grid.Width);
        int goalGridY = (int)(goalPosScreen.Y * grid.Height);
        // 扩展目标区域，使到达更容易
        int goalRadius = 2;
        for (int dx = -goalRadius; dx <= goalRadius; dx++)
        {
            for (int dy = -goalRadius; dy <= goalRadius; dy++)
            {
                int x = goalGridX + dx;
                int y = goalGridY + dy;
                if (grid.IsInBounds(x, y))
                {
                    grid.SetCell(x, y, OccupancyGrid.CellType.Goal);
                }
            }
        }

        int playerGridX = grid.Width / 2;
        int playerGridY = grid.Height / 2;
        grid.SetCell(playerGridX, playerGridY, OccupancyGrid.CellType.Player);
    }

    public void ResetVisitedCells()
    {
        _visitedCells.Clear();
        _previousForce = new Point2f(0, 0);
    }

    // 性能指标计算
    public NavigationPerformanceMetrics CalculatePerformanceMetrics(Point2f playerPos, Point2f goalPos, OccupancyGrid grid)
    {
        var metrics = new NavigationPerformanceMetrics();
        
        // 计算到目标的距离
        metrics.DistanceToGoal = (float)Math.Sqrt(Math.Pow(goalPos.X - playerPos.X, 2) + Math.Pow(goalPos.Y - playerPos.Y, 2));
        
        // 计算障碍物密度
        int obstacleCount = 0;
        int totalCells = grid.Width * grid.Height;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.GetCell(x, y) == OccupancyGrid.CellType.Obstacle)
                {
                    obstacleCount++;
                }
            }
        }
        metrics.ObstacleDensity = (float)obstacleCount / totalCells;
        
        // 计算最近障碍物距离
        float minObstacleDistance = float.MaxValue;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.GetCell(x, y) == OccupancyGrid.CellType.Obstacle)
                {
                    var cellPos = new Point2f((float)x / grid.Width, (float)y / grid.Height);
                    float distance = (float)Math.Sqrt(Math.Pow(cellPos.X - playerPos.X, 2) + Math.Pow(cellPos.Y - playerPos.Y, 2));
                    if (distance < minObstacleDistance)
                    {
                        minObstacleDistance = distance;
                    }
                }
            }
        }
        metrics.MinimumObstacleDistance = minObstacleDistance == float.MaxValue ? 1.0f : minObstacleDistance;
        
        // 计算力场一致性
        var currentForce = ComputeNavigationForce(playerPos, goalPos, grid);
        float forceConsistency = 1.0f - Math.Abs(currentForce.X * _previousForce.X + currentForce.Y * _previousForce.Y);
        metrics.ForceConsistency = Math.Max(0, forceConsistency);
        
        return metrics;
    }
}

// 导航性能指标类
public class NavigationPerformanceMetrics
{
    /// <summary>
    /// 到目标的距离
    /// </summary>
    public float DistanceToGoal { get; set; }
    
    /// <summary>
    /// 障碍物密度
    /// </summary>
    public float ObstacleDensity { get; set; }
    
    /// <summary>
    /// 最近障碍物距离
    /// </summary>
    public float MinimumObstacleDistance { get; set; }
    
    /// <summary>
    /// 力场一致性（0-1）
    /// </summary>
    public float ForceConsistency { get; set; }
    
    /// <summary>
    /// 综合性能评分（0-100）
    /// </summary>
    public float OverallScore
    {
        get
        {
            // 计算综合评分
            float distanceScore = Math.Max(0, 100 - DistanceToGoal * 200);
            float safetyScore = MinimumObstacleDistance * 200;
            float consistencyScore = ForceConsistency * 50;
            float obstacleScore = Math.Max(0, 100 - ObstacleDensity * 200);
            
            return (distanceScore * 0.4f + safetyScore * 0.3f + consistencyScore * 0.2f + obstacleScore * 0.1f);
        }
    }
}

