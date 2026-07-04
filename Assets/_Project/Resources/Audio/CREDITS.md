# Audio Credits / 音效素材署名

按 David 的《游戏声音.xlsx》清单装配（2026-07-02）。CC-BY 素材发行时必须保留本署名。

## 采样素材（原始来源）

| 游戏事件 | 文件 (Resources/Audio/Sfx) | 来源 | 作者 | 许可 |
|---|---|---|---|---|
| 走路/跑步(切片) | footstep_a–d.wav（自 #546827 HQ 预览瞬态对齐切片，2026-07-03 换源重切） | [freesound #546827](https://freesound.org/people/Kinoton/sounds/546827/) "Footsteps Leather Concrete" | Kinoton | CC0 |
| ~~走路(旧源，已退役)~~ | footsteps_walk_raw.mp3（留档：仅 3.6s 片段、木地板/厨房质感不符、页面许可实为 Sampling+ 与此前 CC0 记录不符） | [freesound #52640](https://freesound.org/people/kstein1/sounds/52640/) | kstein1 | Sampling+ |
| 打开柜子 | cabinet_open.wav | [freesound #426765](https://freesound.org/people/cMilan/sounds/426765/) | cMilan | CC0 |
| 关上柜子 | cabinet_close.wav | [freesound #426766](https://freesound.org/people/cMilan/sounds/426766/) | cMilan | CC0 |
| 取出 | pickup_take.mp3 | [freesound #132025](https://freesound.org/people/User1994/sounds/132025/) | User1994 | CC0 |
| **存入** | store_laydown.mp3 | [freesound #245915](https://freesound.org/people/Ediecz/sounds/245915/) | **Ediecz** | **CC-BY**（须署名） |
| **打开电脑** | computer_open.wav | [freesound #39028](https://freesound.org/people/wildweasel/sounds/39028/) | **wildweasel** | **CC-BY**（须署名） |
| **车辆启动** | engine_start.wav | [耳聆网 #15976](https://www.ear0.com/sound/show/soundid-15976) | **耳聆网用户**（丰田4Runner启动出发） | **CC-BY**（须署名） |
| 开门 | door_open.wav（自 #444540 裁剪 0.49s，原 3.15s 大半是静音） | [freesound #444540](https://freesound.org/people/sfarkas92/sounds/444540/) | sfarkas92 | CC0 |
| 关门 | door_close.wav（自 #444541 裁剪 0.43s） | [freesound #444541](https://freesound.org/people/sfarkas92/sounds/444541/) | sfarkas92 | CC0 |
| 掉落 | item_drop.wav | [freesound #361659](https://freesound.org/people/mjvilches/sounds/361659/) | mjvilches | CC0 |

注：freesound 的 door/take 三条为站方公开预览流（HQ mp3）。若要无损原文件，登录 freesound 手动下载替换同名文件即可，代码不用改。

## 程序生成（无版权负担）

| 游戏事件 | 文件 | 生成方式 |
|---|---|---|
| 环境音（烂尾楼/Map2） | ambience_ghost.wav | numpy 合成 60s 无缝循环（棕噪声呼吸底+失谐低频嗡鸣+幽灵气声），替代未下载成功的耳聆 #14713（原文件实为误存的xlsx） |
| 环境音（火星） | ambience_marswind.wav | numpy 合成 45s 无缝循环（阵风+稀疏沙砾tick） |
| 新玩家加入 | synth_join_chime | SynthAudio 运行时合成 |
| 其余未覆盖事件 | synth_* | SynthAudio 运行时合成兜底（悬浮/按住暂未接线） |

## 清单状态 vs《游戏声音.xlsx》

- ✅ 已装配：走路、跑步（同clip高频步点）、打开柜子、取出、存入、关上柜子、打开电脑、车辆启动、开门、关门、环境音、掉落、新玩家加入
- ⏸ 未接线（表内无文件且无明确触发点）：悬浮、按住、上车、下车（上车/下车目前被"车辆启动"整段录音覆盖，内含开关车门声）
