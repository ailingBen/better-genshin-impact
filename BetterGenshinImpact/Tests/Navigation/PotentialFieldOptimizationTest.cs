using System;
using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Navigation;

namespace BetterGenshinImpact.Tests.Navigation;

public class PotentialFieldOptimizationTest
{
    public static void RunOptimizationTest()
    {
        Console.WriteLine("=== 势场法算法参数优化测试 ===");
        Console.WriteLine();

        // 加载默认配置
        var config = new NavigationConfig();
        Console.WriteLine("当前默认配置:");
        Console.WriteLine($"目标引力强度: {config.GoalAttractionStrength}");
        Console.WriteLine($"障碍物斥力强度: {config.ObstacleRepulsionStrength}");
        Console.WriteLine($"障碍物影响半径: {config.ObstacleInfluenceRadius}");
        Console.WriteLine($"路径平滑系数: {config.PathSmoothingFactor}");
        Console.WriteLine($"最大障碍力: {config.MaxObstacleForce}");
        Console.WriteLine($"障碍物安全距离: {config.ObstacleSafetyDistance}");
        Console.WriteLine($"力计算窗口半径: {config.ForceCalculationWindowRadius}");
        Console.WriteLine();

        // 创建优化器
        var optimizer = new PotentialFieldOptimizer(config);

        // 运行优化
        Console.WriteLine("启动参数优化...");
        Console.WriteLine("这可能需要几分钟时间，请耐心等待...");
        Console.WriteLine();

        var result = optimizer.RunOptimization(30); // 减少迭代次数以加快测试速度

        Console.WriteLine();
        Console.WriteLine("=== 优化结果分析 ===");
        Console.WriteLine($"最佳综合评分: {result.BestScore:F2}");
        Console.WriteLine($"测试场景数量: {result.TestScenarios.Count}");
        Console.WriteLine($"评估参数组合数: {result.AllResults.Count}");
        Console.WriteLine();

        // 输出最佳参数配置
        Console.WriteLine("最佳参数配置:");
        Console.WriteLine("=====================================");
        foreach (var param in result.BestParameters)
        {
            Console.WriteLine($"{param.Name}: {param.Value:F4}");
        }
        Console.WriteLine("=====================================");
        Console.WriteLine();

        // 分析参数敏感度
        AnalyzeParameterSensitivity(result);

        // 生成性能报告
        GeneratePerformanceReport(result);

        Console.WriteLine();
        Console.WriteLine("=== 测试完成 ===");
        Console.WriteLine("优化后的参数配置已准备就绪，可以应用到实际导航系统中。");
    }

    private static void AnalyzeParameterSensitivity(OptimizationResult result)
    {
        Console.WriteLine("参数敏感度分析:");
        Console.WriteLine("=====================================");

        // 按参数分组并计算平均评分
        var paramScores = new Dictionary<string, List<float>>();
        foreach (var param in result.BestParameters)
        {
            paramScores[param.Name] = new List<float>();
        }

        foreach (var paramResult in result.AllResults)
        {
            foreach (var param in paramResult.Parameters)
            {
                if (paramScores.ContainsKey(param.Name))
                {
                    paramScores[param.Name].Add(paramResult.AverageScore);
                }
            }
        }

        // 计算每个参数的评分方差，方差越大表示参数越敏感
        foreach (var kvp in paramScores)
        {
            if (kvp.Value.Count > 1)
            {
                float mean = kvp.Value.Average();
                float variance = kvp.Value.Sum(s => (s - mean) * (s - mean)) / (kvp.Value.Count - 1);
                Console.WriteLine($"{kvp.Key}: 敏感度 = {variance:F4}");
            }
        }
        Console.WriteLine("=====================================");
        Console.WriteLine();
    }

    private static void GeneratePerformanceReport(OptimizationResult result)
    {
        Console.WriteLine("性能改进报告:");
        Console.WriteLine("=====================================");

        // 计算平均执行时间
        float avgExecutionTime = result.AllResults.Average(r => r.ExecutionTime);
        float bestExecutionTime = result.AllResults.Min(r => r.ExecutionTime);

        Console.WriteLine($"平均执行时间: {avgExecutionTime:F2}ms");
        Console.WriteLine($"最佳执行时间: {bestExecutionTime:F2}ms");
        Console.WriteLine($"时间优化比例: {((avgExecutionTime - bestExecutionTime) / avgExecutionTime * 100):F2}%");
        Console.WriteLine();

        // 计算评分分布
        var scores = result.AllResults.Select(r => r.AverageScore).ToList();
        float avgScore = scores.Average();
        float maxScore = scores.Max();
        float minScore = scores.Min();

        Console.WriteLine($"平均评分: {avgScore:F2}");
        Console.WriteLine($"最高评分: {maxScore:F2}");
        Console.WriteLine($"最低评分: {minScore:F2}");
        Console.WriteLine($"评分提升比例: {((maxScore - avgScore) / avgScore * 100):F2}%");
        Console.WriteLine("=====================================");
    }

    public static void RunComparisonTest()
    {
        Console.WriteLine();
        Console.WriteLine("=== 不同参数组合性能对比测试 ===");
        Console.WriteLine();

        var configs = new List<NavigationConfig>
        {
            new NavigationConfig { GoalAttractionStrength = 1.0f, ObstacleRepulsionStrength = 5.0f, PathSmoothingFactor = 0.6f },
            new NavigationConfig { GoalAttractionStrength = 1.5f, ObstacleRepulsionStrength = 7.0f, PathSmoothingFactor = 0.8f },
            new NavigationConfig { GoalAttractionStrength = 0.8f, ObstacleRepulsionStrength = 4.0f, PathSmoothingFactor = 0.5f }
        };

        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            var optimizer = new PotentialFieldOptimizer(config);
            
            Console.WriteLine($"测试配置 {i + 1}:");
            Console.WriteLine($"目标引力强度: {config.GoalAttractionStrength}");
            Console.WriteLine($"障碍物斥力强度: {config.ObstacleRepulsionStrength}");
            Console.WriteLine($"路径平滑系数: {config.PathSmoothingFactor}");

            var result = optimizer.RunOptimization(10); // 快速测试
            Console.WriteLine($"综合评分: {result.BestScore:F2}");
            Console.WriteLine();
        }
    }
}
