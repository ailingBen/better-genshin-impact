using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Navigation.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Navigation;

public class PotentialFieldOptimizer
{
    private readonly NavigationConfig _baseConfig;
    private readonly List<OptimizationParameter> _parametersToOptimize;
    private readonly List<TestScenario> _testScenarios;

    public PotentialFieldOptimizer(NavigationConfig baseConfig)
    {
        _baseConfig = baseConfig;
        _parametersToOptimize = new List<OptimizationParameter>
        {
            new OptimizationParameter
            {
                Name = "GoalAttractionStrength",
                MinValue = 0.5f,
                MaxValue = 2.0f,
                Step = 0.2f,
                CurrentValue = baseConfig.GoalAttractionStrength
            },
            new OptimizationParameter
            {
                Name = "ObstacleRepulsionStrength",
                MinValue = 3.0f,
                MaxValue = 10.0f,
                Step = 0.5f,
                CurrentValue = baseConfig.ObstacleRepulsionStrength
            },
            new OptimizationParameter
            {
                Name = "ObstacleInfluenceRadius",
                MinValue = 8.0f,
                MaxValue = 20.0f,
                Step = 2.0f,
                CurrentValue = baseConfig.ObstacleInfluenceRadius
            },
            new OptimizationParameter
            {
                Name = "PathSmoothingFactor",
                MinValue = 0.3f,
                MaxValue = 0.9f,
                Step = 0.1f,
                CurrentValue = baseConfig.PathSmoothingFactor
            },
            new OptimizationParameter
            {
                Name = "MaxObstacleForce",
                MinValue = 1.0f,
                MaxValue = 3.0f,
                Step = 0.2f,
                CurrentValue = baseConfig.MaxObstacleForce
            },
            new OptimizationParameter
            {
                Name = "ObstacleSafetyDistance",
                MinValue = 0.05f,
                MaxValue = 0.2f,
                Step = 0.02f,
                CurrentValue = baseConfig.ObstacleSafetyDistance
            },
            new OptimizationParameter
            {
                Name = "ForceCalculationWindowRadius",
                MinValue = 4,
                MaxValue = 16,
                Step = 2,
                CurrentValue = baseConfig.ForceCalculationWindowRadius
            }
        };

        _testScenarios = new List<TestScenario>
        {
            new TestScenario { Name = "SimplePath", Difficulty = 1, Description = "简单直线路径" },
            new TestScenario { Name = "ObstacleAvoidance", Difficulty = 2, Description = "基本避障场景" },
            new TestScenario { Name = "ComplexMaze", Difficulty = 3, Description = "复杂迷宫场景" },
            new TestScenario { Name = "DynamicObstacles", Difficulty = 4, Description = "动态障碍物场景" }
        };
    }

    public OptimizationResult RunOptimization(int maxIterations = 50)
    {
        var results = new List<ParameterCombinationResult>();
        var bestResult = new ParameterCombinationResult();
        var bestScore = 0.0f;

        Console.WriteLine("开始参数优化...");
        Console.WriteLine($"测试场景数量: {_testScenarios.Count}");
        Console.WriteLine($"优化参数数量: {_parametersToOptimize.Count}");
        Console.WriteLine($"最大迭代次数: {maxIterations}");
        Console.WriteLine();

        // 使用遗传算法进行参数优化
        var population = GenerateInitialPopulation(30);

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Console.WriteLine($"迭代 {iteration + 1}/{maxIterations}");

            // 评估每一组参数
            foreach (var individual in population)
            {
                var config = CreateConfigFromParameters(individual);
                var navigator = new PotentialFieldNavigator(config);
                var scenarioResults = new List<ScenarioResult>();

                foreach (var scenario in _testScenarios)
                {
                    var result = RunScenarioTest(navigator, scenario);
                    scenarioResults.Add(result);
                }

                var combinationResult = new ParameterCombinationResult
                {
                    Parameters = individual,
                    ScenarioResults = scenarioResults,
                    AverageScore = scenarioResults.Average(sr => sr.Score),
                    ExecutionTime = scenarioResults.Average(sr => sr.ExecutionTime)
                };

                results.Add(combinationResult);

                if (combinationResult.AverageScore > bestScore)
                {
                    bestScore = combinationResult.AverageScore;
                    bestResult = combinationResult;
                    Console.WriteLine($"发现更优参数组合，评分: {bestScore:F2}");
                }
            }

            // 选择、交叉、变异
            population = EvolvePopulation(population, results);
        }

        Console.WriteLine();
        Console.WriteLine("优化完成!");
        Console.WriteLine($"最佳参数组合评分: {bestResult.AverageScore:F2}");
        Console.WriteLine($"平均执行时间: {bestResult.ExecutionTime:F2}ms");
        Console.WriteLine();
        Console.WriteLine("最佳参数配置:");
        foreach (var param in bestResult.Parameters)
        {
            Console.WriteLine($"{param.Name}: {param.Value:F4}");
        }

        return new OptimizationResult
        {
            BestParameters = bestResult.Parameters,
            BestScore = bestScore,
            AllResults = results,
            TestScenarios = _testScenarios
        };
    }

    private List<List<OptimizationParameter>> GenerateInitialPopulation(int size)
    {
        var population = new List<List<OptimizationParameter>>();
        for (int i = 0; i < size; i++)
        {
            var individual = _parametersToOptimize.Select(p => new OptimizationParameter
            {
                Name = p.Name,
                MinValue = p.MinValue,
                MaxValue = p.MaxValue,
                Step = p.Step,
                CurrentValue = p.MinValue + (float)new Random().NextDouble() * (p.MaxValue - p.MinValue),
                Value = p.MinValue + (float)new Random().NextDouble() * (p.MaxValue - p.MinValue)
            }).ToList();
            population.Add(individual);
        }
        return population;
    }

    private List<List<OptimizationParameter>> EvolvePopulation(List<List<OptimizationParameter>> population, List<ParameterCombinationResult> results)
    {
        // 简化的遗传算法实现
        var sortedResults = results.OrderByDescending(r => r.AverageScore).ToList();
        var eliteCount = Math.Max(2, sortedResults.Count / 5);
        var newPopulation = sortedResults.Take(eliteCount).Select(r => r.Parameters).ToList();

        // 交叉和变异
        while (newPopulation.Count < population.Count)
        {
            if (sortedResults.Count >= 2)
            {
                var parent1 = sortedResults[new Random().Next(Math.Min(eliteCount, sortedResults.Count))].Parameters;
                var parent2 = sortedResults[new Random().Next(Math.Min(eliteCount, sortedResults.Count))].Parameters;
                var child = Crossover(parent1, parent2);
                Mutate(child);
                newPopulation.Add(child);
            }
            else
            {
                // 如果结果不足，生成新的随机个体
                var randomIndividual = _parametersToOptimize.Select(p => new OptimizationParameter
                {
                    Name = p.Name,
                    MinValue = p.MinValue,
                    MaxValue = p.MaxValue,
                    Step = p.Step,
                    CurrentValue = p.MinValue + (float)new Random().NextDouble() * (p.MaxValue - p.MinValue),
                    Value = p.MinValue + (float)new Random().NextDouble() * (p.MaxValue - p.MinValue)
                }).ToList();
                newPopulation.Add(randomIndividual);
            }
        }

        return newPopulation;
    }

    private List<OptimizationParameter> Crossover(List<OptimizationParameter> parent1, List<OptimizationParameter> parent2)
    {
        var child = new List<OptimizationParameter>();
        for (int i = 0; i < parent1.Count; i++)
        {
            var gene = new Random().NextDouble() > 0.5 ? parent1[i] : parent2[i];
            child.Add(new OptimizationParameter { Name = gene.Name, Value = gene.Value, MinValue = gene.MinValue, MaxValue = gene.MaxValue, Step = gene.Step });
        }
        return child;
    }

    private void Mutate(List<OptimizationParameter> parameters)
    {
        foreach (var param in parameters)
        {
            if (new Random().NextDouble() < 0.1)
            {
                var mutation = (float)(new Random().NextDouble() - 0.5) * param.Step * 2;
                param.Value = Math.Clamp(param.Value + mutation, param.MinValue, param.MaxValue);
            }
        }
    }

    private NavigationConfig CreateConfigFromParameters(List<OptimizationParameter> parameters)
    {
        var config = new NavigationConfig();
        // 复制默认值
        config.GoalAttractionStrength = _baseConfig.GoalAttractionStrength;
        config.ObstacleRepulsionStrength = _baseConfig.ObstacleRepulsionStrength;
        config.ObstacleInfluenceRadius = _baseConfig.ObstacleInfluenceRadius;
        config.ExplorationStrength = _baseConfig.ExplorationStrength;
        config.NavigationInterval = _baseConfig.NavigationInterval;
        config.ShowDebugInfo = _baseConfig.ShowDebugInfo;
        config.ForceCalculationWindowRadius = _baseConfig.ForceCalculationWindowRadius;
        config.PathSmoothingFactor = _baseConfig.PathSmoothingFactor;
        config.MaxObstacleForce = _baseConfig.MaxObstacleForce;
        config.GoalProximityThreshold = _baseConfig.GoalProximityThreshold;
        config.ObstacleSafetyDistance = _baseConfig.ObstacleSafetyDistance;
        config.VelocityDampingFactor = _baseConfig.VelocityDampingFactor;
        config.Enabled = _baseConfig.Enabled;
        config.ConfidenceThreshold = _baseConfig.ConfidenceThreshold;
        config.GridWidth = _baseConfig.GridWidth;
        config.GridHeight = _baseConfig.GridHeight;
        
        foreach (var param in parameters)
        {
            switch (param.Name)
            {
                case "GoalAttractionStrength":
                    config.GoalAttractionStrength = param.Value;
                    break;
                case "ObstacleRepulsionStrength":
                    config.ObstacleRepulsionStrength = param.Value;
                    break;
                case "ObstacleInfluenceRadius":
                    config.ObstacleInfluenceRadius = param.Value;
                    break;
                case "PathSmoothingFactor":
                    config.PathSmoothingFactor = param.Value;
                    break;
                case "MaxObstacleForce":
                    config.MaxObstacleForce = param.Value;
                    break;
                case "ObstacleSafetyDistance":
                    config.ObstacleSafetyDistance = param.Value;
                    break;
                case "ForceCalculationWindowRadius":
                    config.ForceCalculationWindowRadius = (int)param.Value;
                    break;
            }
        }
        return config;
    }

    private ScenarioResult RunScenarioTest(PotentialFieldNavigator navigator, TestScenario scenario)
    {
        var stopwatch = new Stopwatch();
        var metricsList = new List<NavigationPerformanceMetrics>();

        // 创建测试场景的占用网格
        var grid = new OccupancyGrid(80, 60);
        SetupScenarioGrid(grid, scenario);

        var playerPos = new Point2f(0.1f, 0.5f);
        var goalPos = new Point2f(0.9f, 0.5f);

        stopwatch.Start();
        for (int i = 0; i < 10; i++)
        {
            var metrics = navigator.CalculatePerformanceMetrics(playerPos, goalPos, grid);
            metricsList.Add(metrics);
            
            // 模拟移动
            var force = navigator.ComputeNavigationForce(playerPos, goalPos, grid);
            playerPos += force * 0.05f;
            playerPos.X = Math.Clamp(playerPos.X, 0, 1);
            playerPos.Y = Math.Clamp(playerPos.Y, 0, 1);
        }
        stopwatch.Stop();

        var averageScore = metricsList.Average(m => m.OverallScore);
        var executionTime = stopwatch.ElapsedMilliseconds / 10.0f;

        return new ScenarioResult
        {
            ScenarioName = scenario.Name,
            Score = averageScore,
            ExecutionTime = executionTime,
            Metrics = metricsList
        };
    }

    private void SetupScenarioGrid(OccupancyGrid grid, TestScenario scenario)
    {
        switch (scenario.Name)
        {
            case "SimplePath":
                // 简单直线路径，无障碍物
                break;
            case "ObstacleAvoidance":
                // 基本避障场景，中间有一个障碍物
                for (int x = 35; x < 45; x++)
                {
                    for (int y = 25; y < 35; y++)
                    {
                        grid.SetCell(x, y, OccupancyGrid.CellType.Obstacle);
                    }
                }
                break;
            case "ComplexMaze":
                // 复杂迷宫场景
                for (int x = 0; x < grid.Width; x += 10)
                {
                    for (int y = 0; y < 20; y++)
                    {
                        grid.SetCell(x, y, OccupancyGrid.CellType.Obstacle);
                    }
                }
                for (int x = 5; x < grid.Width; x += 10)
                {
                    for (int y = 40; y < grid.Height; y++)
                    {
                        grid.SetCell(x, y, OccupancyGrid.CellType.Obstacle);
                    }
                }
                break;
            case "DynamicObstacles":
                // 动态障碍物场景模拟
                for (int x = 20; x < 30; x++)
                {
                    for (int y = 10; y < 50; y++)
                    {
                        grid.SetCell(x, y, OccupancyGrid.CellType.Obstacle);
                    }
                }
                for (int x = 50; x < 60; x++)
                {
                    for (int y = 10; y < 50; y++)
                    {
                        grid.SetCell(x, y, OccupancyGrid.CellType.Obstacle);
                    }
                }
                break;
        }
    }
}

public class OptimizationParameter
{
    public string Name { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
    public float Step { get; set; }
    public float CurrentValue { get; set; }
    public float Value { get; set; }
}

public class TestScenario
{
    public string Name { get; set; }
    public int Difficulty { get; set; }
    public string Description { get; set; }
}

public class ScenarioResult
{
    public string ScenarioName { get; set; }
    public float Score { get; set; }
    public float ExecutionTime { get; set; }
    public List<NavigationPerformanceMetrics> Metrics { get; set; }
}

public class ParameterCombinationResult
{
    public List<OptimizationParameter> Parameters { get; set; }
    public List<ScenarioResult> ScenarioResults { get; set; }
    public float AverageScore { get; set; }
    public float ExecutionTime { get; set; }
}

public class OptimizationResult
{
    public List<OptimizationParameter> BestParameters { get; set; }
    public float BestScore { get; set; }
    public List<ParameterCombinationResult> AllResults { get; set; }
    public List<TestScenario> TestScenarios { get; set; }
}
