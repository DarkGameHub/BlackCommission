# 低保真"糊感"诊断与修正优先级

**版本**: 1.0
**日期**: 2026-06-10
**作者**: 美术总监（banach）
**触发**: PM 试玩反馈"感觉有点太糊了"
**依据资产**: `URP-Pipeline.asset`、`URP-Renderer.asset`、`DefaultVolumeProfile.asset`、场景 camera 设置

---

## 一、当前渲染栈实测参数

| 参数 | 当前值 | 读取来源 |
|------|--------|---------|
| renderScale | **0.5** | URP-Pipeline.asset `m_RenderScale` |
| 上采样模式 | **2（Nearest/Point）** | `m_UpscalingFilter: 2` |
| MSAA | **Off（1）** | `m_MSAA: 1` |
| 摄像机 AA | **None（0）** | 所有场景 camera `m_Antialiasing: 0` |
| IntermediateTextureMode | **Always（1）** | URP-Renderer.asset `m_IntermediateTextureMode: 1` |
| 深度贴图（Pipeline 级） | **未开启** | `m_RequireDepthTexture: 0` |
| Bloom intensity | **0（关闭）** | DefaultVolumeProfile |
| DepthOfField mode | **0（关闭）** | DefaultVolumeProfile |
| MotionBlur intensity | **0（关闭）** | DefaultVolumeProfile |
| FilmGrain intensity | **0（关闭）** | DefaultVolumeProfile |
| 描边注入点 | **550（AfterRenderingPostProcessing）** | URP-Renderer.asset `injectionPoint` |
| 描边所需资源 | **7（Depth+Normal+Color）** | `requirements: 7` |

**结论**：后处理层面无任何已知模糊源（Bloom/DoF/MotionBlur 全关）。"糊"不来自 Volume 后处理。

---

## 二、各维度诊断

### 2.1 renderScale 0.5 的有效分辨率

| 原生分辨率 | renderScale 0.5 渲染分辨率 | 每轴像素损失 |
|-----------|--------------------------|------------|
| 1080p（1920×1080） | **960×540** | -50% |
| 1440p（2560×1440） | **1280×720** | -50% |
| 4K（3840×2160） | **1920×1080** | -50% |

**LC 等效对比**：Lethal Company 固定渲染分辨率 860×520，在 1080p 屏幕上
等效 renderScale ≈ 0.45（860/1920）。BC 的 0.5 与 LC 相近，不是"比 LC 更糊"的问题。

**但关键差异**：LC 用 HDRP 固定分辨率方式，上采样前后的像素映射是 1:1 整数倍（或接近）。
URP 的 renderScale 系统是浮点缩放，Point 上采样在**非整数倍分辨率**下会产生不均匀的纹素块，
部分像素被混入相邻块——这不是理论上"脆的硬边纹素"，而是带有轻微空间抖动的"脏块"。

**0.5 在 1080p 下是 2:1 整数倍，是最干净的 Point 上采样状态。** 如果 PM 在 1440p 或非
1080p 屏幕上试玩，1280×720→1440p 的 Point 放大是非整数倍（2.0×1.0，垂直 2× 但水平约 2×），
理论上在标准宽高比下也是整数倍，问题不大。

**renderScale 0.5 本身不是糊的主因，但有一个例外——见 2.3。**

---

### 2.2 主嫌：IntermediateTextureMode = Always（最高优先级排查）

`URP-Renderer.asset` 中 `m_IntermediateTextureMode: 1`（Always）。

这意味着 URP 在 Point 上采样**之前**，强制将渲染结果先 blit 到一个中间 Render Texture，
再执行 Final Blit 到屏幕。这个中间 blit 路径在某些 Unity 6 URP 版本中**使用 Bilinear 过滤**，
直接抵消了 Point 上采样的脆边效果。

**症状特征**：整个画面呈轻微模糊，纹素边界被软化，但不是高斯模糊式的大范围"化开"——
这正好符合 PM 的"糊"而不是"太亮"或"失焦"的描述。

**修复方法**：将 `m_IntermediateTextureMode` 改为 `0`（Auto）。Auto 模式下 URP 只在
必要时引入中间 RT（如某些 Renderer Feature 需要），大多数情况下直接走 Final Blit。

> 注意：本项目的 LC_Retro_Outline FullScreenPassRendererFeature 设置了
> `requirements: 7`（Depth+Normal+Color），这会让 URP 在 Forward 渲染路径下
> 强制引入中间 RT（为了满足 fetchColorBuffer 需求）。切换到 Auto 后，
> 中间 RT 仍会存在，但 URP 会尝试优化 blit 过滤器选择。
> 若改 Auto 后仍糊，考虑在 FullScreenPass 中显式设置上采样为 Point（见 2.5）。

---

### 2.3 次嫌：贴图双线性过滤 × 256px × 2m 平铺

256px 贴图在 2m 世界平铺下，1m≈128 纹素，在 0.5 renderScale 的 1080p（960×540）画面中，
FOV 60° 时 1m 距离的物体约占 ~180 屏幕像素。**纹素：屏幕像素 ≈ 1:1.4**。

- **Bilinear 过滤**：每个纹素采样时平均与相邻纹素混合，在此比例下会产生明显柔化。
  这是"贴图糊"而不是"渲染糊"——靠近看更明显，退后反而好一些。
- **Point 过滤**：每个纹素硬边，但在 1:1.4 映射比例下，屏幕像素落在纹素边界时会产生
  轻微的半像素锯齿（阶梯感）——低保真风格里这是特征，不是缺陷。

**研究报告原结论（路径4）需要修正范围扩大**：
报告建议"大面墙/地板保持双线性，道具改 Point"。但在 0.5 renderScale 的放大倍率下，
**大面 Bilinear 是比道具更严重的糊感来源**——大面占画面面积最大，主观糊感主要来自此处。

---

### 2.4 描边注入点问题（低优先，但影响糊感感知）

`injectionPoint: 550` = `AfterRenderingPostProcessing`。

描边在后处理之后注入，绘制在已经经过 Final Blit 的帧上，边缘线条是在全分辨率下画的。
这实际上是**正确的**——描边锐利度不受 renderScale 影响。

但若 IntermediateTextureMode 的中间 blit 在描边之前发生了模糊，描边会盖在糊的底图上，
反而让"有描边但底图糊"的对比更突出，放大 PM 的糊感知觉。

描边本身设置（强度 0.6，颜色 `#0A0F14`）无需调整。

---

### 2.5 上采样过滤器关键字未启用（辅助排查）

`m_PrefilterPointSamplingUpsampling: 0` — URP Pipeline Asset 中点采样上采样的
shader 预编译关键字处于关闭状态。

Unity 6 URP 的 Point 上采样依赖 `_POINT_SAMPLING_UPSCALING` shader keyword。
若该关键字没有被正确 strip（或未变体编译），运行时实际使用的可能仍是 Bilinear 路径，
即使 Inspector 里显示的是 Nearest（Point）模式。

**验证方法**：进入 Unity Editor > Window > Analysis > Frame Debugger，
找到最终 Blit Pass，检查 Shader Keywords 列表中是否包含 `_POINT_SAMPLING_UPSCALING`。
若无，说明上采样实际走的是 Bilinear，Point 设置未生效。

---

## 三、推荐修正顺序（先什么后什么）

### Step 1【30分钟，最高优先】：IntermediateTextureMode Auto + Frame Debugger 验证

**操作**：
- `Assets/Settings/URP-Renderer.asset` > Inspector > Intermediate Texture > 改为 **Auto**
- 进入 Frame Debugger 确认最终 Blit 的 Shader Keywords 包含 `_POINT_SAMPLING_UPSCALING`
- 截图对比改前后的墙面纹素边界清晰度

**预期效果**：如果 Intermediate blit 是主因，改完后画面纹素边界立即变脆，
PM 的"糊感"应显著改善，不需要改 renderScale。

**风险**：Auto 模式可能因 LC_Retro_Outline 的 fetchColorBuffer 需求仍触发中间 RT。
若如此，进入 FullScreenPassRendererFeature，尝试将 `fetchColorBuffer` 改为 false
（需在描边 shader 中改为采样 _CameraColorAttachmentA），从根本上消除中间 RT 触发条件。

---

### Step 2【15分钟，并行可做】：大面建筑贴图 Filter Mode 改 Point

**操作**：
- 在 Unity Project 窗口选中墙面/地板/顶板的 albedo 贴图
- Texture Import Settings > Filter Mode > 改为 **Point（No Filter）**
- 同步关闭 Generate Mip Maps（mipmap 本身是另一个双线性柔化源，在低保真固定近距离下不需要）

**参考规格**（扩大研究报告路径4的范围）：

| 贴图类型 | Filter Mode | Mip Maps |
|---------|------------|---------|
| 大面环境（墙/地/顶） | **Point** | 关闭 |
| 道具/设备 | Point（原已建议） | 关闭 |
| 贴花 decal | Point | 关闭 |
| UI 字体贴图（TMP atlas） | Bilinear | 保持默认 |

**注意**：关闭 Mip Maps 后远距离贴图会出现摩尔纹（纹理闪烁）。BC 是走廊/室内场景，
观看距离有限（极少超过 20m），在低保真风格下摩尔纹被接受为颗粒质感。
若不可接受，改为 Mip Maps 开启但 Filter Mode 仍 Point（会在每级 mip 边界产生硬跳变，
也是低保真特征）。

---

### Step 3【按需，若 Step 1+2 后仍觉得糊】：renderScale 调整至 0.6

**操作**：`URP-Pipeline.asset` > Render Scale 改为 **0.6**。

| renderScale | 1080p 渲染 | 1440p 渲染 | 性能开销变化 |
|------------|-----------|-----------|------------|
| 0.5 | 960×540 | 1280×720 | 基准 |
| 0.6 | 1152×648 | 1536×864 | +44%像素数 |
| 0.67 | 1286×724 | ~1715×963 | +79%像素数 |

**0.6 的权衡**：
- 像素数增加 44%，GPU 开销增加，但 BC 低保真风格下几何面少、材质简单，
  有性能余量吸收这部分开销
- 在 1080p 下 1.667:1 放大比不是整数倍，Point 上采样会出现轻微不均匀像素块
  （在 1440p 下是 2.222:1，同样非整数倍）
- 0.6 的"颗粒感"会比 0.5 细腻，更接近 LC 的视觉密度，但可见纹素仍然清晰

**建议**：如果 Step 1 修复了 Intermediate Blit 问题，0.5 的纹素颗粒感会非常明显，
可能不需要改 renderScale。Step 3 是"Step 1 确认无效后才做的调整"。

---

### Step 4【albedo 贴图处理，美术侧，与渲染无关】：Posterize 量化增加对比度

即使 Step 1+2 修复了渲染层面的糊，albedo 本身若渐变过多，
双线性→Point 切换后纹素颗粒会更"干净"但仍缺乏低保真色块感。

按研究报告路径1执行：Posterize 4步（大面）或 6步（道具），加 3-5% 噪点。
这步不影响运行时糊感，但能增加纹素颗粒的视觉信息密度，强化低保真审美意图。

---

## 四、描边参数是否需要随之调整

Step 1+2 修复后，纹素变脆，描边下方的底图对比度提升，描边本身会显得更突出。

建议在确认 Step 1 效果后：
- 若描边感觉"太重"（底图变清晰后轮廓线太抢），将强度从 **0.6 降至 0.4**
- 若描边感觉"正好"，保持不变
- 颜色 `#0A0F14` 保持，这是 BC 身份色板的组成部分

描边参数调整是美术视觉判断，不需要技术改动。

---

## 五、糊感来源优先级总览

| 优先级 | 来源 | 类型 | 修复成本 | 预期改善 |
|--------|------|------|---------|---------|
| P1 | IntermediateTextureMode = Always | 渲染管线 | 5分钟 | 高 |
| P1 | Point 上采样 keyword 未激活（待验证） | 渲染管线 | 10分钟 | 高 |
| P2 | 大面贴图 Bilinear + Mip Maps | 贴图导入 | 30分钟 | 中-高 |
| P3 | renderScale 0.5→0.6 | 管线参数 | 5分钟 | 低-中 |
| P4 | albedo 缺乏量化对比度 | 贴图制作 | 5min/张 | 中（不影响糊，影响信息密度） |

---

## 六、不是糊的来源（排除项）

- **Bloom**：intensity = 0，完全关闭，排除
- **DepthOfField**：mode = 0（关闭），排除
- **MotionBlur**：intensity = 0，排除
- **FilmGrain**：intensity = 0，排除（注：如需"颗粒感"这里可开启，但不影响糊感）
- **MSAA**：Off，排除
- **摄像机 AA（FXAA/TAA）**：所有场景 m_Antialiasing = 0（None），排除
- **renderScale 0.5 的分辨率**：在 1080p 下是 2:1 整数倍，Point 上采样理论干净，排除为主因
