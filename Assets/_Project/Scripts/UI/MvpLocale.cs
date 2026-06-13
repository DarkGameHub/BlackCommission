using System.Collections.Generic;
using UnityEngine;

public static class MvpLocale
{
    static int Lang => PlayerPrefs.GetInt("AS.Settings.Language", 0);

    static readonly Dictionary<string, string[]> Strings = new()
    {
        // index 0 = English (default), index 1 = 中文 (简体)

        // ─── QuickNetworkUI ───
        ["host"] = new[] { "Host", "主机" },
        ["client"] = new[] { "Client", "客户端" },
        ["connected_status"] = new[] { "Online  {0}  Players {1}", "在线  {0}  玩家 {1}" },
        ["room_code_share"] = new[] { "Room Code (share with teammates)", "房间号（分享给队友）" },
        ["room_code_join_hint"] = new[] { "Teammates enter this code to join", "队友输入此号码加入" },
        ["hide"] = new[] { "Hide", "隐藏" },
        ["subtitle"] = new[] { "Outsourced Commission Office", "外包委托事务所" },
        ["create_office"] = new[] { "Create Office", "创建事务所" },
        ["join_office"] = new[] { "Join Office", "加入事务所" },
        ["create_hint"] = new[] { "Create and share the room code with teammates.", "创建房间并将房间号分享给队友。" },
        ["hide_direct"] = new[] { "▾ Hide Direct Connect", "▾ 隐藏直连" },
        ["show_direct"] = new[] { "▸ Direct Connect (LAN)", "▸ 直连（局域网）" },
        ["address"] = new[] { "Addr", "地址" },
        ["direct_create"] = new[] { "Direct Host", "直连主机" },
        ["direct_join"] = new[] { "Direct Join", "直连加入" },
        ["enter_code"] = new[] { "Enter Room Code", "输入房间号" },
        ["ask_host_code"] = new[] { "Ask host for 6-digit code", "向主机询问 6 位房间号" },
        ["join"] = new[] { "Join", "加入" },
        ["back"] = new[] { "Back", "返回" },
        ["connecting"] = new[] { "Connecting", "连接中" },
        ["please_wait"] = new[] { "Please wait", "请稍候" },
        ["enter_code_prompt"] = new[] { "Please enter a room code.", "请输入房间号。" },
        ["code_six_chars"] = new[] { "Room code is 6 characters.", "房间号为 6 位。" },
        ["relay_unavailable"] = new[] { "Relay unavailable, use direct connect.", "中继不可用，请使用直连。" },
        ["host_started"] = new[] { "Host started.", "主机已启动。" },
        ["host_failed"] = new[] { "Failed to create host.", "主机创建失败。" },
        ["direct_host_started"] = new[] { "Direct host started: {0}:{1}", "直连主机已启动：{0}:{1}" },
        ["host_port_busy"] = new[] { "Host failed: port in use.", "主机失败：端口被占用。" },
        ["port_error"] = new[] { "Invalid port format.", "端口格式无效。" },
        ["joining"] = new[] { "Joining {0}:{1}...", "正在加入 {0}:{1}..." },
        ["join_failed"] = new[] { "Failed to join.", "加入失败。" },

        // ─── Player identity / lobby roster ───
        ["player_name"] = new[] { "Agent Name", "探员名称" },
        ["roster_title"] = new[] { "Squad {0}/4", "小队 {0}/4" },
        ["you_tag"] = new[] { "(you)", "（你）" },
        ["room_label"] = new[] { "Room {0}", "房间 {0}" },
        ["lobby_waiting_title"] = new[] { "Crew Standby", "待命中" },
        ["lobby_waiting_hint"] = new[] { "Enter the office, accept a commission at the computer, then gather at the van.", "进入事务所，在电脑处接受委托，然后在厢式车旁集合。" },
        ["lobby_room_code"] = new[] { "Room Code", "房间号" },
        ["lobby_lan_only"] = new[] { "LAN Direct", "局域网直连" },
        ["lobby_enter_office"] = new[] { "Enter Office", "进入事务所" },
        ["lobby_client_note"] = new[] { "Wait for the host to pick a job; you can enter and check gear.", "等待主机接单；你可以进入检查装备。" },
        ["lobby_empty_slot"] = new[] { "Empty - waiting", "空位 - 等待中" },

        // ─── Main menu (flat LC-style, mockup B) ───
        ["crt_terminal_header"] = new[] { "BC-DOS v2.2 — BLACK COMMISSION", "BC-DOS v2.2 — 黑色委托事务所" },
        ["menu_continue"] = new[] { "Continue Shift", "继续营业" },
        ["menu_new_office"] = new[] { "New Office", "新事务所" },
        ["menu_settings"] = new[] { "Settings", "设置" },
        ["menu_shutdown"] = new[] { "Shut Down", "关机" },
        ["crt_no_save"] = new[] { "(no save)", "（无存档）" },
        ["crt_online"] = new[] { "SYSTEM ONLINE — ROSTER ISSUED", "系统在线 — 派工名单已生成" },
        ["job_note_header"] = new[] { "NEW JOB", "新委托" },
        ["job_note_client"] = new[] { "Client: {0}", "委托人：{0}" },
        ["job_note_reward"] = new[] { "Reward {0}G", "报酬 {0}G" },
        ["lan_direct_link"] = new[] { "▸ LAN direct…", "▸ 局域网直连…" },

        // ─── Connection events ───
        ["host_disconnected"] = new[] { "Host disconnected. Returning...", "主机已断线，正在返回..." },
        ["player_joined"] = new[] { "A teammate joined", "一位队友加入了" },
        ["player_left"] = new[] { "A teammate left", "一位队友离开了" },
        ["you_were_kicked"] = new[] { "You were removed by the host.", "你被主机移出了房间。" },
        ["local_mode_only"] = new[] { "Online service unavailable: switched to local mode (LAN direct only, no internet join).", "在线服务不可用：已切换至本地模式（仅局域网直连，无法通过互联网加入）。" },

        // ─── VanTransitOverlay ───
        ["commission"] = new[] { "Commission", "委托" },
        ["mission_location"] = new[] { "Mission Site", "任务地点" },
        ["van_cabin"] = new[] { "Van Rear Cabin", "厢式车后舱" },
        ["all_aboard"] = new[] { "All aboard {0}/{1}", "全员到齐 {0}/{1}" },
        ["waiting_team"] = new[] { "Waiting... {0}/{1}", "等待队友... {0}/{1}" },
        ["driver_waiting"] = new[] { "Driver waiting up front", "司机在前面等着" },
        ["press_e_drive"] = new[] { "Press [E] to drive", "按 [E] 开走" },
        ["press_space_depart"] = new[] { "[Space] Depart", "[空格] 发车" },
        // 离座 (not 下车): "下车" is reserved for the arrival disembark prompt ([E] 下车) —
        // one word per action across the whole boarding-transit chain.
        ["press_x_leave"] = new[] { "[X] Leave seat", "[X] 离座" },
        ["departing_in"] = new[] { "Departing in {0}… (stragglers left behind)", "发车倒计时 {0}…（未上车者将被留下）" },
        ["wait_all_aboard"] = new[] { "Everyone must be aboard to depart", "等待所有人上车才能发车" },
        ["sit_in_van"] = new[] { "Press [E] to board and sit", "按 [E] 上车就座" },
        ["dispatch_outbound"] = new[] { "Dispatching to site", "派车去现场" },
        ["return_office"] = new[] { "Returning to office", "返程回事务所" },
        ["office"] = new[] { "Office", "事务所" },
        ["in_van"] = new[] { "In van {0}/4", "车内 {0}/4" },

        // ─── MvpHud: Settings ───
        ["pause"] = new[] { "Paused", "已暂停" },
        ["resume"] = new[] { "Resume", "继续" },
        ["game"] = new[] { "Game", "游戏" },
        ["language"] = new[] { "Language: {0}", "语言：{0}" },
        ["master_volume"] = new[] { "Master Volume: {0}", "主音量：{0}" },
        ["show_network"] = new[] { "Show Network Hints", "显示网络提示" },
        ["camera"] = new[] { "Camera", "相机" },
        ["h_sensitivity"] = new[] { "Horizontal Sensitivity: {0}", "水平灵敏度：{0}" },
        ["v_sensitivity"] = new[] { "Vertical Sensitivity: {0}", "垂直灵敏度：{0}" },
        ["invert_y"] = new[] { "Invert Y-Axis", "反转 Y 轴" },
        ["fov"] = new[] { "Field of View: {0}", "视野：{0}" },
        ["display"] = new[] { "Display", "显示" },
        ["brightness"] = new[] { "Brightness: {0}", "亮度：{0}" },
        ["fullscreen"] = new[] { "Fullscreen", "全屏" },
        ["quality"] = new[] { "Quality: {0}", "画质：{0}" },
        ["voice"] = new[] { "Voice", "语音" },
        ["voice_default_on"] = new[] { "Voice Enabled by Default", "默认启用语音" },
        ["mute_self"] = new[] { "Mute Self", "静音自己" },
        ["push_to_talk"] = new[] { "Push-to-Talk (hold V)", "按键通话（按住 V）" },
        ["mic_device"] = new[] { "Microphone: {0}", "麦克风：{0}" },
        ["mic_gain"] = new[] { "Mic Gain: {0}", "麦克风增益：{0}" },
        ["voice_volume"] = new[] { "Voice Volume: {0}", "语音音量：{0}" },
        ["voice_distance"] = new[] { "Voice Range: {0} m", "语音范围：{0} 米" },
        ["reset_defaults"] = new[] { "Reset Defaults", "恢复默认" },
        ["quit_game"] = new[] { "Quit Game", "退出游戏" },
        ["prev"] = new[] { "Prev", "上一个" },
        ["next"] = new[] { "Next", "下一个" },

        // ─── MvpHud: Office Panel ───
        ["reward_pending"] = new[] { "Reward pending: {0}. Claim at computer.", "待领取奖励：{0}。在电脑处领取。" },
        ["task_accepted"] = new[] { "Commission accepted: {0}. Buy gear, then to van.", "已接受委托：{0}。购买装备后前往厢式车。" },
        ["computer_connected"] = new[] { "Computer online. Accept jobs or buy gear.", "电脑在线。接受任务或购买装备。" },
        ["office_idle"] = new[] { "Office on standby.", "事务所待机中。" },
        ["terminal_title"] = new[] { "BLACK COMMISSION TERMINAL", "黑委托终端" },
        ["close_computer"] = new[] { "Close", "关闭" },
        ["funds_debt"] = new[] { "Funds: {0} G    Debt: {1} G", "资金：{0} G    债务：{1} G" },
        ["rep_level_xp"] = new[] { "Rep: {0}    Level: {1}    XP: {2}/{3}", "声望：{0}    等级：{1}    经验：{2}/{3}" },
        ["unlocked_categories"] = new[] { "Unlocked categories: {0}/8", "已解锁类别：{0}/8" },
        ["takeover_pressure"] = new[] { "Takeover Pressure: {0}/100", "收购压力：{0}/100" },
        ["lost_item_progress"] = new[] { "Lost Item Jobs: {0}/2", "遗失物任务：{0}/2" },

        // ─── MvpHud: Warnings ───
        ["hostile_acquired"] = new[] { "Warning: Office acquired by competitor. Debt increased, progress reset.", "警告：事务所被竞争者收购。债务增加，进度重置。" },
        ["hostile_acquired_hint"] = new[] { "Earn money and reputation to reduce future takeover risk.", "赚取资金和声望以降低今后的收购风险。" },
        ["ultimatum_issued"] = new[] { "Competitor threat: Office under ultimatum.", "竞争者威胁：事务所处于最后通牒中。" },
        ["ultimatum_hint"] = new[] { "Raise funds or reputation before the next failure.", "在下次失败前提高资金或声望。" },
        ["ultimatum_active"] = new[] { "Ultimatum: Another failure with negative funds/rep means forced acquisition.", "最后通牒：再次失败且资金/声望为负将被强制收购。" },
        ["ultimatum_resolve"] = new[] { "Complete commissions or raise funds/rep to lift the threat.", "完成委托或提高资金/声望以解除威胁。" },

        // ─── MvpHud: Rewards ───
        ["pending_reward"] = new[] { "Pending: {0}  Money {1} / Rep {2} / XP {3}", "待领取：{0}  金钱 {1} / 声望 {2} / 经验 {3}" },
        ["claim_reward"] = new[] { "Claim Reward", "领取奖励" },
        ["wait_host_claim"] = new[] { "Waiting for host to claim", "等待主机领取" },
        ["host_only_claim"] = new[] { "Only the host can confirm. Team syncs after.", "仅主机可确认。团队随后同步。" },

        // ─── MvpHud: Acquisition ───
        ["acquisition_available"] = new[] { "Expansion: Acquire Level 0 office for {0}G.", "扩张：以 {0}G 收购零级事务所。" },
        ["confirm_acquire"] = new[] { "Confirm Acquisition", "确认收购" },
        ["acquisition_cost_hint"] = new[] { "Requires sufficient funds and pressure below 70.", "需要足够资金且压力低于 70。" },
        ["acquisition_done"] = new[] { "Expansion complete: Level 0 office acquired. Second category unlocked.", "扩张完成：零级事务所已收购。第二类别已解锁。" },
        ["acquisition_hint"] = new[] { "Keep doing Lost Item jobs to build funds and reputation.", "持续接取遗失物任务以积累资金和声望。" },

        // ─── MvpHud: Commission ───
        ["task_locked"] = new[] { "Accepted: {0}", "已接受：{0}" },
        ["task_locked_hint"] = new[] { "Buy gear, then head to the company van.", "购买装备后前往公司厢式车。" },
        ["available_commissions"] = new[] { "Available Commissions", "可接委托" },
        ["terminal_offline"] = new[] { "Terminal offline.", "终端离线。" },
        ["accept_task"] = new[] { "Accept Commission", "接受委托" },
        ["wait_host_accept"] = new[] { "Waiting for host to accept", "等待主机接受" },
        ["wait_host_hint"] = new[] { "Waiting for host to select.", "等待主机选择。" },
        ["start_host_first"] = new[] { "Start Host to accept", "请先启动主机" },
        ["start_host_hint"] = new[] { "Start host first.", "请先启动主机。" },

        // ─── MvpHud: Shop ───
        ["shop_title"] = new[] { "Computer Shop", "电脑商店" },
        ["claim_first"] = new[] { "Claim your pending reward before buying gear.", "购买装备前请先领取待领奖励。" },
        ["shop_hotkeys"] = new[] { "Click to buy items near the computer. Press G to drop items.", "在电脑附近点击购买物品。按 G 丢弃物品。" },
        ["shop_stand_near"] = new[] { "Stand near the computer to buy. Press G anywhere in HQ to drop items.", "站在电脑旁购买。在总部任意位置按 G 丢弃物品。" },
        ["insufficient_funds"] = new[] { "Insufficient funds: {0} costs {1}G.", "资金不足：{0} 需要 {1}G。" },
        ["cant_store"] = new[] { "{0} can't be stored: {1}", "{0} 无法存储：{1}" },
        ["purchase_submitted"] = new[] { "Purchase submitted: {0}, syncing.", "购买已提交：{0}，同步中。" },
        ["purchase_stamped"] = new[] { "Purchase approved: {0} -{1}G.", "购买已批准：{0} -{1}G。" },
        ["purchase_failed"] = new[] { "{0} purchase failed.", "{0} 购买失败。" },
        ["watch_owned"] = new[] { "You already have a cheap wristwatch.", "你已经拥有一块廉价腕表。" },
        ["watch_no_funds"] = new[] { "Insufficient funds: Wristwatch costs {0}G.", "资金不足：腕表需要 {0}G。" },
        ["watch_submitted"] = new[] { "Purchase submitted: Wristwatch, syncing.", "购买已提交：腕表，同步中。" },
        ["watch_stamped"] = new[] { "Purchase approved: Wristwatch -{0}G.", "购买已批准：腕表 -{0}G。" },
        ["watch_failed"] = new[] { "Wristwatch purchase failed.", "腕表购买失败。" },
        ["watch_owned_label"] = new[] { "Wristwatch Equipped", "腕表已装备" },
        ["watch_buy_label"] = new[] { "Wristwatch  {0}G", "腕表  {0}G" },

        // ─── Item names ───
        ["flashlight"] = new[] { "Flashlight", "手电筒" },
        ["battery"] = new[] { "Battery", "电池" },

        // ─── MvpHud: Mission ───
        ["downed_spectating"] = new[] { "Downed — Spectating: {0}  [LMB/RMB to switch]", "倒地 — 观战：{0}  [左键/右键切换]" },
        ["downed_all"] = new[] { "Downed — All players down", "倒地 — 所有玩家倒地" },

        // ─── MvpHud: Mission Van ───
        ["mission_van"] = new[] { "Van Rear Cabin", "厢式车后舱" },
        ["close_door"] = new[] { "Close", "关闭" },
        ["van_decide_hint"] = new[] { "Decide whether to return first; locker supplies are optional.", "决定是否先返回；储物柜补给为可选项。" },
        ["van_locker"] = new[] { "Van Locker", "车载储物柜" },
        ["take_item"] = new[] { "Take", "取用" },
        ["locker_request"] = new[] { "Requesting: {0}.", "申请中：{0}。" },
        ["continue_search"] = new[] { "Continue Search", "继续搜索" },
        ["partial_return_warn"] = new[] { "Warning: Clicking return again sends everyone back for partial pay.", "警告：再次点击返回将使所有人带着部分奖励回营。" },

        // ─── PlayerHealth ───
        ["revive_progress"] = new[] { "Reviving ({0}%)", "复活中（{0}%）" },
        ["revive_hint"] = new[] { "Revive Teammate", "复活队友" },

        // ─── MvpHud: Field Clock ───
        ["wristwatch_time"] = new[] { "Watch: {0}    Deadline: {1}", "腕表：{0}    截止：{1}" },
        ["daylight_label"] = new[] { "Daylight: {0}", "日光：{0}" },
        ["no_watch_hint"] = new[] { "Check the van clock or buy a wristwatch for exact time.", "查看厢式车时钟或购买腕表以获取准确时间。" },
        ["overtime_feeling"] = new[] { "You feel you've gone past the contract window. Penalties apply.", "你感觉已超出合同时限。将被扣减奖励。" },
        ["van_clock"] = new[] { "Van clock: {0}    End of shift: {1}", "车载时钟：{0}    班次结束：{1}" },
        ["remaining_window"] = new[] { "Remaining: {0}", "剩余：{0}" },
        ["overtime_penalty"] = new[] { "Overtime: {0}    Penalty -{1}G / Rep -{2}", "加班：{0}    扣减 -{1}G / 声望 -{2}" },
        ["overtime_reward_penalty"] = new[] { "Overtime penalty: {0}  -{1}G / Rep -{2}", "加班扣减：{0}  -{1}G / 声望 -{2}" },
        ["wristwatch_status_owned"] = new[] { "Watch equipped", "腕表已装备" },
        ["wristwatch_status_none"] = new[] { "No watch", "无腕表" },
        ["hotbar_summary"] = new[] { "Hotbar: {0}/{1}  Flashlight x{2} / Battery x{3}  {4}", "快捷栏：{0}/{1}  手电筒 x{2} / 电池 x{3}  {4}" },
    };

    public static string T(string key)
    {
        if (Strings.TryGetValue(key, out string[] values))
        {
            int lang = Mathf.Clamp(Lang, 0, values.Length - 1);
            return values[lang];
        }
        return key;
    }

    public static string T(string key, params object[] args)
    {
        string template = T(key);
        return string.Format(template, args);
    }
}
