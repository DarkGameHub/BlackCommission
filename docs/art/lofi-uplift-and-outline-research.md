# Lo-Fi 提升与描边研究报告

**版本**: 1.0  
**日期**: 2026-06-10  
**作者**: 美术总监（banach）  
**适用画风锁定**: `black-commission-style-lock-v2.md`  
**状态**: 可执行规格

---

## 问题A：纹理再提升一档（保持 ≤256px 低保真路线）

### A-1 现状评估

当前白盒材质基线：ambientCG albedo 降采样至 256px + 色调 tint + 高粗糙（smoothness ≤0.3），URP Lit，无 normal/AO/metalness 贴图。

视觉问题诊断：
- 表面整体过于"干净"——albedo 降采样后颗粒感有了，但缺乏层次，脏旧感依赖单一贴图的随机性
- 没有局部信息密度差异：角落、接缝、破损处与大面没有视觉区分
- 平铺缝隙和重复感在玩法尺度（1-4m 范围内）比较明显

### A-2 五条提升路径（优先级排序）

---

#### 路径 1【推荐·立竿见影】：颜色量化 Posterize 处理基础 Albedo

**原理**：将 256px albedo 在 Photoshop / Aseprite 里做色阶量化（Posterize，步骤数 4-6），压缩渐变为阶梯状色块，纹素颗粒感更强烈，视觉上更接近低保真色调版画。

**操作规格**：
- 量化步骤数：4（大面墙/地）或 6（道具/设备，保留少量细节）
- 量化后叠加 3-5% 噪点（Photoshop Filter > Add Noise，Monochromatic）防止纯平块
- 保持 256px 分辨率不变，仅改像素值分布
- 导出格式：PNG，sRGB，无 mipmap 偏移

**与 BC 色板的关系**：量化时用混凝土灰/军绿/旧木的近似色限制色阶，确保量化后的颜色仍在 art-bible 色板范围内。

**成本**：每张贴图约 5 分钟处理，无渲染开销。

---

#### 路径 2【推荐·层次感最大化】：双层脏旧（Albedo + 顶点色积尘）

**原理**：在统一 albedo 贴图之上，用网格顶点色（Vertex Color）在角部/接缝处叠加脏旧覆盖层（暗化 + 轻微饱和度降低）。这是 Lethal Company 等低保真游戏控制环境表面层次的主要手段之一，无需额外 UV 通道或 AO 贴图。

**实现方式（委托 technical-artist 写 URP Lit 变体 shader）**：

```
// 伪代码逻辑
float dirtMask = vertexColor.r;           // 建模时在角部/接缝刷深色顶点色
float3 dirtColor = float3(0.08, 0.07, 0.06); // 碳灰积尘色
albedo = lerp(albedo, albedo * dirtColor, dirtMask * _DirtIntensity);
```

**顶点色工作流**：
- 在 Blender 中为模块化墙/柱/地板套件刷顶点色
- 红通道（R）= 积尘强度：角部 0.6-0.8，大面 0-0.1
- 仅刷环境建筑模块，道具保持干净（叙事：道具是"有人最近用过的"）
- 需要同步更新 `docs/art/current-playable-hq-pipeline.md` 中材质说明

**成本**：需要 shader 变体（技术美术工作量约 2-3h），Blender 顶点色刷每个模块约 15-30 分钟。

---

#### 路径 3【中期·信息密度最高】：Decal 贴花（水渍/裂缝/BC 标语）

**原理**：URP 内置 Decal Renderer Feature（Unity 6 已稳定，支持 Deferred 和 Forward+）。在环境关键表面投射独立的脏旧/破损/叙事 decal，不依赖底层 UV 就能增加局部细节密度。

**BC 具体 decal 清单（优先制作）**：

| 分类 | 资产名称示例 | 内容 | 尺寸 |
|------|------------|------|------|
| 水渍 | `env_decal_waterstain_01` | 混凝土水渍环 | 128px |
| 裂缝 | `env_decal_crack_corner_01` | 墙角裂缝 | 128px |
| 标语 | `env_decal_debt_stamp_01` | 印章红"欠款"/"催收"汉字 | 256px |
| 污渍 | `env_decal_grime_streak_01` | 顺竖方向黑色污痕 | 128px |
| 法律标识 | `env_decal_notice_yellow_01` | 老旧安全黄警告边框 | 256px |

**技术规格**：
- 所有 decal 使用 DXT5/BC3 Alpha 通道控制遮罩
- Blend Mode: Alpha Blend，Surface Data: Albedo only（不改 normals，保持低保真感）
- 每场景 decal 实例数上限：50（性能预算，由 technical-artist 确认）

**成本**：需启用 Decal Renderer Feature；每张贴花制作约 20-40 分钟。

---

#### 路径 4【可选·全局观感统一】：点过滤（Point Filter）vs 双线性（Bilinear）

**观感差异**：
- **双线性（当前默认）**：像素间平滑过渡，256px 贴图看起来"模糊柔和"
- **点过滤（Point/Nearest）**：像素硬边，256px 贴图每个纹素都有清晰边界，产生像素画质感

**BC 的推荐选择**：
- 大面墙/地板：保持**双线性**——硬边纹素在大面积重复时会产生过于明显的格子感
- 道具/交互物（柜子、箱子、设备）：改为**点过滤**——强化"廉价工业品"的颗粒质感，与玩家近距离交互时更有视觉特征
- 贴花/标语：**点过滤**——让文字和图案的像素感更明显，符合"便宜打印/手工贴上去"的叙事

**修改方法**：在 Unity Inspector 的 Texture Import Settings > Filter Mode 中切换，零代码成本。

---

#### 路径 5【可选·效率工具】：Trim Sheet 减少 UV 复杂度

**原理**：将常用的边缘/角部/接缝细节集中在一张 256px 条带贴图（trim sheet）上，环境模块的边缘 UV 映射到此贴图，减少独立材质数量同时增加边缘细节。

**适用范围**：适合大量复用的模块化墙/地板套件，不适合已经批量生产的现有单件道具。

**推荐延迟**：待当前模块化套件稳定后（Sprint 3+）再引入，以免返工。

---

### A-3 材质 Shader 模式选择

| Shader | 适用场景 | 理由 |
|--------|---------|------|
| URP Lit | 接受直接光/阴影的大面建筑表面 | 正确响应场景光（钨丝橙黄/冷白手电），灯光语法生效 |
| URP SimpleLit | 小道具、背景道具 | 减少渲染计算，低保真风格中 SimpleLit 的平坦感反而是优势 |
| URP BakedLit | 纯静态背景装饰，不动、不被手电照到的表面 | 零运行时光照成本，适合建筑外观远景 |

**推荐**：当前白盒继续用 URP Lit；体积感不重要的小道具迁移至 SimpleLit 节省 drawcall 计算。

---

## 问题B：Lethal Company"黑色轮廓"技术构成及 Unity 6 URP 落地方案

> 红线说明：以下所有分析仅针对渲染技术构成，不复制 LC 任何可见资产（飞船、怪物、UI、地图等）。

### B-1 LC 渲染技术确认（基于社区分析及 80.lv 技术拆解）

Lethal Company 使用 **Unity HDRP**（非 URP），其视觉风格由以下组件叠加构成。经 Acerola 视频分析及社区 mod 逆向确认，技术栈如下：

**技术组件贡献度排序**（从视觉冲击最大到最小）：

| 排名 | 技术组件 | 贡献描述 | LC 中的实现方式 |
|------|---------|---------|----------------|
| 1 | **像素化降采样 + 上采样** | 最核心。以 860×520 固定分辨率渲染后上采样至原生分辨率，产生硬边纹素感 | HDRP Camera 固定渲染分辨率 + 点过滤上采样 |
| 2 | **后处理边缘检测描边** | "黑色轮廓"的直接来源。基于深度缓冲 + 法线缓冲做差分检测，描边绘制在几何轮廓和曲面断层处 | 自写 HDRP Custom Pass（非 Unity 内置 outline） |
| 3 | **Posterization 色调分级** | 将体积光/环境色阶压缩为可数色阶（约 4-6 档），产生平面化渲染感。注意：LC 的 posterization **作用于体积光照数据，而非原始 albedo** | HDRP 自写 Custom Pass |
| 4 | **Vignette 暗角** | 强化"廉价监控摄像头/头盔摄像"感，也是黑暗边缘控制的重要手段 | URP/HDRP 内置 Post Processing Volume |
| 5 | **Bloom（低阈值）** | 光源轻微光晕，配合钨丝橙黄灯光产生"廉价灯泡"感。LC 中 bloom 参数比较克制 | HDRP 内置 |
| 6 | **Depth of Field（头盔视差模拟）** | 模拟廉价镜头的对焦感，近处轻微失焦。LC 特有的"头盔摄像机"叙事组件 | HDRP 内置，非必要移植 |
| 7 | **色彩矫正 / LUT** | 整体色调偏灰绿，高对比度，暗部不细腻 | HDRP Color Grading Volume |

**关键确认：描边不是 Backface 膨胀**

LC 的黑色轮廓是**后处理深度+法线边缘检测**（Post-Process Edge Detection based on Depth and Normals），而不是：
- 传统背面膨胀描边（Backface Hull Outline）
- 法线外扩（Normal Extrusion）
- 菲涅尔边缘着色

判断依据：LC 描边出现在**同一物体的曲面断层处**（如管道顶部），不仅仅是物体轮廓，这是 depth/normal edge detection 的典型特征；背面膨胀法无法检测内部几何断层。

---

### B-2 BC 的黑色轮廓：不完全复制 LC，基于 BC 身份的变体方案

LC 的描边是"玩家头盔摄像机"叙事的组成部分。BC 没有头盔，是第一人称公务员视角。因此描边的叙事定位应调整：

**BC 描边设计意图**：强化工业空间的结构可读性 + 辅助黑暗环境中的几何轮廓识别 + 低保真视觉身份感。不需要"廉价摄像机"感（那是 LC 的叙事）。

**推荐参数方向**：描边比 LC 略薄，颜色可以不是纯黑（考虑深市政蓝-黑 `#0A0F14`），只在强曲率断层和物体边界处出现，室内光照充足区域描边可淡出。

---

### B-3 Unity 6 URP（RenderGraph 时代）落地方案排序

Unity 6（6000.4.7f1）的 URP 17 已全面迁移至 RenderGraph API，Compatibility Mode（旧 ScriptableRenderPass）标记为将被废弃。以下方案按**推荐优先级**排序：

---

#### 方案 1【最推荐】：FullScreenPassRendererFeature + Fullscreen Shader Graph

**适用版本**：Unity 6 URP 17（6000.4+），完全兼容 RenderGraph，官方维护。

**工作流**：
1. 创建 Fullscreen Shader Graph（Material Type = Fullscreen）
2. 在 Shader Graph 中编写深度+法线边缘检测逻辑（可使用 Scene Depth 节点和 Scene Normals 节点）
3. 在 URP Renderer Data 中添加 `FullScreenPassRendererFeature`
4. 设置 Pass Material 为上述 Fullscreen 材质
5. 注入点选择 `After Opaques`（描边在透明物体之前绘制）

**优势**：
- 无需写 C# ScriptableRendererFeature 代码，门槛最低
- FullScreenPassRendererFeature 内部已适配 RenderGraph（源码确认使用 `renderGraph.AddRasterRenderPass`）
- Shader Graph 可视化节点，便于参数调试
- Unity 6 官方文档支持，稳定性有保障

**劣势**：
- Shader Graph 对 depth/normal sampling 的控制略不如手写 HLSL 精细
- 复杂的多采样算法（如 Roberts Cross 9点采样）在 Shader Graph 中节点较多，可读性下降

**委托事项**：将以下规格交给 technical-artist 实现。

---

#### 方案 2【备选，控制最精细】：自写 ScriptableRendererFeature + HLSL Shader

**适用场景**：需要精细控制采样模式、多 pass 或条件渲染时。

**注意**：Unity 6 URP 要求使用 RenderGraph API 重写 ScriptableRenderPass。旧式 `Configure()` + `Execute()` 模式仍可通过 Compatibility Mode 运行，但建议直接用 RenderGraph 接口：

```csharp
// Unity 6 RenderGraph 风格（伪代码骨架）
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();
    using (var builder = renderGraph.AddRasterRenderPass<PassData>("EdgeDetectionOutline", out var passData))
    {
        passData.sourceTexture = resourceData.activeColorTexture;
        passData.depthTexture = resourceData.cameraDepthTexture;
        builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
        builder.UseTexture(passData.depthTexture, AccessFlags.Read);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) => ExecutePass(data, ctx));
    }
}
```

**劣势**：工程量较大（约 4-6h），更适合 technical-artist 主导，不适合美术自行调参。

---

#### 方案 3【不推荐】：Asset Store 第三方包（如 CONTOUR）

社区有 "CONTOUR - Edge detection & outline post effect for Unity 6 URP Render Graph" 等现成包，标注兼容 Unity 6 RenderGraph。

**为何不推荐**：
- 外部依赖引入维护风险
- 参数和描边风格已被包作者预设，难以完全贴合 BC 的市政黑暗美学
- 涉及 URP Renderer Data 和 RenderGraph 的外部 Feature 升级时容易冲突

---

### B-4 边缘检测算法选择

BC 推荐使用 **Roberts Cross（罗伯茨交叉算子）** 而不是 Sobel：

| 算法 | 采样数 | 边缘特征 | BC 适配性 |
|------|--------|---------|---------|
| Roberts Cross | 4点（2×2） | 细边、低计算量、轻微对角偏向 | 推荐。细边更符合 BC"工业结构线"而非 LC 的粗轮廓 |
| Sobel | 9点（3×3） | 中等粗细、各向均匀 | 备选，更接近 LC 原版效果 |
| TriangleDepthNormals | 3点 | 最轻量、边缘最细 | 道具快速 prototype 用 |

**双通道结合**：同时采样深度缓冲和法线缓冲，用 `max()` 合并，可同时检测：
- 物体边界轮廓（深度突变）
- 曲面内部断层（法线方向突变）

---

### B-5 参数起点规格表

以下为 BC 黑色描边的**推荐起点参数**，由 technical-artist 实现后美术调参：

| 参数 | 推荐起点值 | 范围 | 说明 |
|------|----------|------|------|
| **描边颜色** | `#0A0F14`（深市政蓝黑） | — | 非纯黑，带 BC 色板身份 |
| **描边强度（Outline Scale）** | 0.6 | 0.1 – 2.0 | 控制描边可见度 |
| **深度阈值（Depth Threshold）** | 1.5 | 0.1 – 5.0 | 深度差超过此值才描边；值越低描边越多 |
| **法线阈值（Normal Threshold）** | 0.4 | 0.001 – 10.0 | 法线差超过此值才描边；0.4 适合低多边形几何 |
| **深度采样偏移（Depth Bias）** | 0.005 | 0 – 0.05 | 防止 Z-fighting 产生自描边噪点 |
| **法线采样偏移（Normal Bias）** | 0.0 | 0 – 0.1 | 通常 0 即可 |
| **像素化倍率（可选）** | 降采样至 960×540，点过滤上采样 | — | 若启用像素化全屏效果（见下方） |
| **Posterize 步骤数（可选）** | 5 | 3 – 8 | 作用于最终光照数据，非 albedo |

---

### B-6 像素化降采样后处理（可选，低优先）

LC 的核心像素化来自渲染分辨率锁定。BC 当前不需要全局像素化（style-lock v2 明确"不做极端 PS1 像素恐怖/不做仿 CRT 全屏滤镜"）。

若未来需要局部像素化（如 HQ 安保显示器、van 内的监控屏），推荐使用独立 Render Texture + Camera 而非全屏后处理，隔离范围影响。

---

### B-7 实现路线图（按优先级）

| 优先级 | 任务 | 执行者 | 预估工时 | 依赖 |
|--------|------|--------|---------|------|
| P1 | 现有 albedo 贴图做 posterize 量化处理（路径1） | 美术 | 0.5h/批次 | 无 |
| P1 | 道具贴图 Filter Mode 改为 Point（路径4） | 美术 | 0.5h | 无 |
| P2 | 方案1 FullScreenPassRendererFeature + Fullscreen Shader Graph 描边实现 | technical-artist | 3-4h | Unity 6 URP 17 |
| P2 | 调参至 B-5 规格表起点值，美术验收 | 美术+TA | 1h | P2 TA 任务完成 |
| P3 | 顶点色积尘 shader 变体（路径2） | technical-artist | 2-3h | 无 |
| P3 | 模块化套件顶点色刷制 | 美术 | 2h/套 | P3 TA 任务完成 |
| P4 | Decal 贴花制作：水渍/裂缝/BC 标语 | 美术 | 1-2h/张 | Decal RF 启用 |

---

## 附录：参考来源

- [How to Achieve Lethal Company's Graphics With Unity HDRP — 80.lv](https://80.lv/articles/how-to-achieve-lethal-company-s-graphics-with-unity-hdrp)
- [The Strange Graphics Of LETHAL COMPANY — Acerola（YouTube）](https://www.youtube.com/watch?v=Z_-am00EXIc)
- [Graphics Analysis of Lethal Company — Coconote（Acerola 视频文字稿）](https://coconote.app/notes/02c78513-4735-481a-b383-9be78e438a07)
- [CONTOUR - Edge detection & outline post effect for Unity 6 URP Render Graph](https://discussions.unity.com/t/contour-edge-detection-outline-post-effect-for-unity-6-urp-render-graph/1563657)
- [Edge Detection Outlines — ameye.dev（Roberts Cross 算子参数参考）](https://ameye.dev/notes/edge-detection-outlines/)
- [Converting Screen Space Outline Renderer Feature to Render Graph — Unity Discussions](https://discussions.unity.com/t/converting-screen-space-outline-renderer-feature-to-render-graph/949671)
- [Unity 6 URP Upgrade Guide（URP 17 RenderGraph）](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/upgrade-guide-unity-6.html)
- [Fullscreen Shader Graph Reference for URP](https://docs.unity3d.com/Manual//urp/prebuilt-shader-graphs-urp-fullscreen.html)
- [Posterize Node — Shader Graph 文档](https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Posterize-Node.html)
