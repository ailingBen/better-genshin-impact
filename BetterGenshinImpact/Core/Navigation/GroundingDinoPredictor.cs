using System;
using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.Core.Navigation.Model;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Navigation;

public class GroundingDinoPredictor : IDisposable
{
    private readonly BgiOnnxModel _model;
    private readonly Lazy<InferenceSession> _lazySession;
    private bool _disposed;

    protected internal GroundingDinoPredictor(BgiOnnxModel onnxModel, string modelPath, SessionOptions sessionOptions)
    {
        _model = onnxModel;
        _lazySession = new Lazy<InferenceSession>(() => new InferenceSession(modelPath, sessionOptions));
    }

    public InferenceSession Session => _lazySession.Value;

    public List<DetectedObject> Detect(ImageRegion region, string textPrompt, float confidenceThreshold = 0.5f)
    {
        var detections = new List<DetectedObject>();

        try
        {
            var image = region.SrcMat;
            var resized = image.Resize(new OpenCvSharp.Size(640, 640));
            var inputTensor = PreprocessImage(resized);
            var textEmbedding = GetTextEmbedding(textPrompt);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", inputTensor),
                NamedOnnxValue.CreateFromTensor("text", textEmbedding)
            };

            using var results = Session.Run(inputs);
            detections = PostProcessResults(results, image.Size(), confidenceThreshold, textPrompt);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GroundingDINO detection error: {ex.Message}");
        }

        return detections;
    }

    private DenseTensor<float> PreprocessImage(Mat image)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, 640, 640 });

        for (int y = 0; y < 640; y++)
        {
            for (int x = 0; x < 640; x++)
            {
                var pixel = image.At<Vec3b>(y, x);
                tensor[0, 0, y, x] = (pixel.Item2 / 255.0f - 0.485f) / 0.229f;
                tensor[0, 1, y, x] = (pixel.Item1 / 255.0f - 0.456f) / 0.224f;
                tensor[0, 2, y, x] = (pixel.Item0 / 255.0f - 0.406f) / 0.225f;
            }
        }

        return tensor;
    }

    private DenseTensor<float> GetTextEmbedding(string text)
    {
        var embedding = new DenseTensor<float>(new[] { 1, 256, 768 });
        return embedding;
    }

    private List<DetectedObject> PostProcessResults(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results, OpenCvSharp.Size originalSize, float threshold, string label)
    {
        var detections = new List<DetectedObject>();

        var boxes = results.FirstOrDefault(r => r.Name == "boxes")?.AsEnumerable<float>().ToArray();
        var logits = results.FirstOrDefault(r => r.Name == "logits")?.AsEnumerable<float>().ToArray();

        if (boxes != null && logits != null)
        {
            var scaleX = (float)originalSize.Width / 640;
            var scaleY = (float)originalSize.Height / 640;

            for (int i = 0; i < logits.Length; i += 1)
            {
                if (logits[i] > threshold)
                {
                    var boxIndex = i * 4;
                    if (boxIndex + 3 < boxes.Length)
                    {
                        var x1 = boxes[boxIndex] * scaleX;
                        var y1 = boxes[boxIndex + 1] * scaleY;
                        var x2 = boxes[boxIndex + 2] * scaleX;
                        var y2 = boxes[boxIndex + 3] * scaleY;

                        detections.Add(new DetectedObject
                        {
                            BBox = new Rect((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1)),
                            Label = label,
                            Confidence = logits[i],
                            Depth = 0.5f
                        });
                    }
                }
            }
        }

        return detections;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_lazySession.IsValueCreated)
            {
                Session.Dispose();
            }
            _disposed = true;
        }
    }

    ~GroundingDinoPredictor()
    {
        Dispose();
    }
}
