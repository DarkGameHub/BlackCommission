# Monster Art Credits / 怪物美术素材署名

按 David 拍板的「现成可商用资产改皮」路线（2026-07-02）。

| 游戏怪物 | 文件 | 基础模型来源 | 作者 | 许可 |
|---|---|---|---|---|
| 档案怨灵（FileWarden 视觉v2） | ArchiveWraith.fbx（原名 Ghost Skull，含8段骨骼动画） | [Quaternius — Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) | Quaternius | CC0（可商用，无须署名；此处留档致谢） |

## 改皮说明

- `ArchiveWraith_Atlas.png` = 原 Flying 系 `Atlas_Monsters.png` 经
  `tools/rigging/recolor_atlas.py` 按亮度重映射为项目调色：
  dead rubber black → civic teal → aged paper 渐变，红系色块 → 印章红。
- 材质/控制器/prefab 由 `FileWardenSetup.cs` 一键重建（导入缩放 0.55 → 全高 ~1.78m）。
- 骷髅眼窝的红光来自 prefab 内 `SealGlow` 点光（威胁语言 = 图章红），非贴图自发光。

## 自建资产（无版权负担）

| 怪物 | 来源 |
|---|---|
| 回声霉菌 EchoMold | tools/rigging/build_echo_mold.py（Blender 程序化自建） |
| FileWarden v1（已退役，FBX 留档） | tools/rigging/build_file_warden.py |
