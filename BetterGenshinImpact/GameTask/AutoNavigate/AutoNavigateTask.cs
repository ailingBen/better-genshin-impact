using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Navigation;
using BetterGenshinImpact.Core.Navigation.Model;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Vanara.PInvoke;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoNavigate;

public class AutoNavigateTask
{
    private readonly CancellationToken _ct;
    private readonly PotentialFieldNavigator _navigator;
    private GroundingDinoPredictor? _dinoPredictor;
    private OccupancyGrid _grid;

    public int DetectionInterval { get; set; } = 3;
    public int GridWidth { get; set; } = 80;
    public int GridHeight { get; set; } = 60;
    public string ObstaclePrompt { get; set; } = "enemy, wall, obstacle";
    public string GoalPrompt { get; set; } = "door, exit, treasure";

    public AutoNavigateTask(CancellationToken ct, NavigationConfig? config = null)
    {
        _ct = ct;
        _navigator = new PotentialFieldNavigator(config ?? new NavigationConfig());
        _grid = new OccupancyGrid(GridWidth, GridHeight);
    }

    public async Task StartAsync(Point2f goalPosScreen)
    {
        Logger.LogInformation("开始自动导航...");
        _navigator.ResetVisitedCells();

        int frameCount = 0;
        List<DetectedObject> detections = new();

        while (!_ct.IsCancellationRequested)
        {
            frameCount++;

            using var region = CaptureToRectArea();

            if (frameCount % DetectionInterval == 0)
            {
                detections = DetectObjects(region);
                _navigator.UpdateOccupancyGrid(_grid, detections, goalPosScreen);
                Logger.LogDebug($"检测到 {detections.Count} 个对象");
            }

            var playerPos = new Point2f(0.5f, 0.5f);
            var force = _navigator.ComputeNavigationForce(playerPos, goalPosScreen, _grid);

            MoveCharacter(force);

            await Delay(50, _ct);
        }

        StopMovement();
        Logger.LogInformation("自动导航结束");
    }

    private List<DetectedObject> DetectObjects(ImageRegion region)
    {
        var allDetections = new List<DetectedObject>();

        try
        {
            if (_dinoPredictor != null)
            {
                var obstacles = _dinoPredictor.Detect(region, ObstaclePrompt, 0.5f);
                allDetections.AddRange(obstacles);

                var goals = _dinoPredictor.Detect(region, GoalPrompt, 0.5f);
                allDetections.AddRange(goals);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"检测失败: {ex.Message}");
        }

        return allDetections;
    }

    private void MoveCharacter(Point2f force)
    {
        if (Math.Abs(force.X) < 0.1f && Math.Abs(force.Y) < 0.1f)
        {
            StopMovement();
            return;
        }

        var angle = Math.Atan2(force.Y, force.X);
        var magnitude = Math.Sqrt(force.X * force.X + force.Y * force.Y);

        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_W);
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_A);
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_S);
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_D);

        if (magnitude > 0.1f)
        {
            var degrees = angle * (180.0 / Math.PI);

            if (degrees is > -45 and <= 45)
            {
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_W);
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_D);
            }
            else if (degrees is > 45 and <= 135)
            {
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_W);
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_A);
            }
            else if (degrees is > -135 and <= -45)
            {
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_S);
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_D);
            }
            else
            {
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_S);
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_A);
            }
        }
    }

    private void StopMovement()
    {
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_W);
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_A);
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_S);
        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_D);
    }

    public void SetGroundingDinoPredictor(GroundingDinoPredictor predictor)
    {
        _dinoPredictor = predictor;
    }

    public void SetGridSize(int width, int height)
    {
        GridWidth = width;
        GridHeight = height;
        _grid = new OccupancyGrid(width, height);
    }
}
