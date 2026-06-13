---
name: project-style-lock
description: BC 画风已锁定为低保真工业恐怖（lo-fi low-poly），style-lock-v2 是执行标准，art-bible 保留但保真度条款被 v2 修正
metadata:
  type: project
---

Black Commission 画风：**低保真合作工业恐怖**（2026-06-10 PM Yan Dai 拍板，style-lock-v2）。

关键约束：
- 贴图分辨率上限 256px；可见纹素是特征不是缺陷
- 不做半写实 PBR（v1 废止），不做极端 PS1 顶点抖动
- 色板：混凝土灰主调，钨丝橙黄 `#FFAB40` 主点缀，印章红 `#C23A2B` 仅纸面，CRT 绿仅电子屏
- 灯光：三种光源 5000K 冷白 / 3000K 钨丝暖 / CRT 绿；手电筒主照明

**Why:** PM 在 v1 半写实路线后回归 retro 低保真方向。  
**How to apply:** 任何资产/材质提案都先对照 style-lock-v2 §3–§4 的规则，禁止引入 normal/AO/metalness 微细节贴图，禁止 1K 以上贴图。
