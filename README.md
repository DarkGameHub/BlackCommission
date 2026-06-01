# 黑色外包（Black Commission）

黑色外包是一款 1-4 人合作外包接单游戏——一家快要倒闭的事务所靠接各种越来越离谱的外包任务维持运营。

当前 MVP 核心流程：

1. 单机开房或创建/加入联机房间。
2. 出生在破旧的事务所办公室。
3. 用办公室电脑接受任务。
4. 进入学校关卡，找回丢失的作业本，躲开学校异常体，撤离到出口。
5. 返回事务所，结算金钱/声望/经验，花钱购买装备、恢复道具、事务所升级或未来的机构收购。

完整 MVP 设计、故事背景、小队配置及第一阶段实现计划见 [docs/mvp-core-loop.md](docs/mvp-core-loop.md)。

当前美术方向已锁定，见 [docs/art/black-commission-style-lock-v1.md](docs/art/black-commission-style-lock-v1.md)。

## Unity 工程启动

1. 若是首次 checkout，先运行 `Tools > Black Commission > Setup All (Run This First!)`。
2. 运行 `Tools > Black Commission > MVP > Setup School MVP`。
3. 运行 `Tools > Black Commission > MVP > Validate School MVP`。
4. 打开 `HQ` 场景，按 Play，点击 `Start Host`，然后用办公室电脑进入学校任务。

## 生成美术工作流

1. 在 Windows 且已安装 Blender 的环境下运行：
   ```
   blender --background --factory-startup --python D:/BlackCommission/docs/art/blender_outsourced_civic_commercial_v4.py
   ```
2. 在 Unity 中运行 `Tools > Black Commission > Art > Import Generated Blender Kit`。
3. 导入的 Prefab 会生成到 `Assets/_Project/Prefabs/Art`。
