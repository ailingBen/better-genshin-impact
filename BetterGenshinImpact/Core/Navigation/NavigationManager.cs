using System;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.ONNX;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Navigation;

public class NavigationManager : IDisposable
{
    private readonly ILogger<NavigationManager> _logger;
    private readonly BgiOnnxFactory _onnxFactory;
    private readonly AllConfig _config;
    private GroundingDinoPredictor? _dinoPredictor;
    private PotentialFieldNavigator _navigator;
    private bool _disposed;

    public bool IsInitialized { get; private set; }

    public NavigationManager(ILogger<NavigationManager> logger, BgiOnnxFactory onnxFactory, AllConfig config)
    {
        _logger = logger;
        _onnxFactory = onnxFactory;
        _config = config;
        _navigator = new PotentialFieldNavigator(config.NavigationConfig);
    }

    public bool Initialize()
    {
        try
        {
            _logger.LogInformation("初始化导航系统...");

            if (!BgiOnnxModel.IsModelExist(BgiOnnxModel.GroundingDino))
            {
                _logger.LogWarning("GroundingDINO 模型文件不存在，导航系统将使用模拟模式运行");
                IsInitialized = true;
                return true;
            }

            _dinoPredictor = _onnxFactory.CreateGroundingDinoPredictor(BgiOnnxModel.GroundingDino);
            _logger.LogInformation("GroundingDINO 模型加载成功");

            IsInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航系统初始化失败");
            return false;
        }
    }

    public void SetDinoPredictor(GroundingDinoPredictor predictor)
    {
        _dinoPredictor = predictor;
    }

    public GroundingDinoPredictor? GetDinoPredictor()
    {
        return _dinoPredictor;
    }

    public PotentialFieldNavigator GetNavigator()
    {
        return _navigator;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _dinoPredictor?.Dispose();
            _disposed = true;
        }
    }

    ~NavigationManager()
    {
        Dispose();
    }
}
