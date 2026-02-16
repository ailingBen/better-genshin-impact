# GroundingDINO 模型设置指南

## 方法一：使用预导出的 ONNX 模型（推荐）

### 1. 寻找第三方预导出模型

由于官方没有直接提供 GroundingDINO 的 ONNX 版本，您可以在以下平台寻找预导出的模型：

- **GitHub Releases**：搜索 "GroundingDINO ONNX"
- **Hugging Face**：https://huggingface.co/models?search=groundingdino
- **Modelscope**：https://www.modelscope.cn/models

### 2. 下载并放置模型

将下载的 `groundingdino.onnx` 文件放置到以下目录：

```
BetterGenshinImpact/Assets/Model/Navigation/groundingdino.onnx
```

如果目录不存在，请手动创建。

---

## 方法二：自行导出 ONNX 模型

### 1. 环境准备

需要安装 Python 3.8+ 和 PyTorch。

```bash
# 克隆 GroundingDINO 仓库
git clone https://github.com/IDEA-Research/GroundingDINO.git
cd GroundingDINO

# 安装依赖
pip install -e .
pip install onnx onnxruntime
```

### 2. 下载 PyTorch 预训练模型

```bash
mkdir weights
cd weights
wget https://github.com/IDEA-Research/GroundingDINO/releases/download/v0.1.0-alpha/groundingdino_swint_ogc.pth
cd ..
```

### 3. 创建导出脚本

在 GroundingDINO 目录下创建 `export_onnx.py`：

```python
import torch
from groundingdino.models import build_model
from groundingdino.util import box_ops
from groundingdino.util.slconfig import SLConfig
from groundingdino.util.utils import clean_state_dict

def load_model(model_config_path, model_checkpoint_path, device="cpu"):
    args = SLConfig.fromfile(model_config_path)
    args.device = device
    model = build_model(args)
    checkpoint = torch.load(model_checkpoint_path, map_location="cpu")
    load_res = model.load_state_dict(clean_state_dict(checkpoint["model"]), strict=False)
    print(load_res)
    _ = model.eval()
    return model

# 加载模型
model = load_model(
    "groundingdino/config/GroundingDINO_SwinT_OGC.py",
    "weights/groundingdino_swint_ogc.pth"
)

# 示例输入
dummy_image = torch.randn(1, 3, 640, 640)
dummy_text = torch.randn(1, 256, 768)

# 导出 ONNX
torch.onnx.export(
    model,
    (dummy_image, dummy_text),
    "groundingdino.onnx",
    export_params=True,
    opset_version=12,
    do_constant_folding=True,
    input_names=["images", "text"],
    output_names=["boxes", "logits"],
    dynamic_axes={
        "images": {0: "batch_size"},
        "text": {0: "batch_size"},
        "boxes": {0: "batch_size"},
        "logits": {0: "batch_size"}
    }
)

print("ONNX 模型导出成功！")
```

### 4. 运行导出

```bash
python export_onnx.py
```

### 5. 放置模型

将生成的 `groundingdino.onnx` 复制到：

```
BetterGenshinImpact/Assets/Model/Navigation/groundingdino.onnx
```

---

## 验证模型

运行项目后，检查日志中是否有关于 GroundingDINO 模型加载成功的信息。

## 注意事项

- GroundingDINO 模型较大（约 2GB），请确保有足够的磁盘空间
- 首次加载模型可能需要较长时间
- 建议使用 GPU 加速（DirectML 或 CUDA）以获得更好的性能
