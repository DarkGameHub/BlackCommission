using System.Collections.Generic;
using UnityEngine;

public static class MvpLocale
{
    static int Lang => PlayerPrefs.GetInt("AS.Settings.Language", 0);

    static readonly Dictionary<string, string[]> Strings = new()
    {
        // ─── QuickNetworkUI ───
        ["host"] = new[] { "房主", "Host" },
        ["client"] = new[] { "客户端", "Client" },
        ["connected_status"] = new[] { "已联机  {0}  玩家 {1}", "Online  {0}  Players {1}" },
        ["room_code_share"] = new[] { "房间代码 (分享给队友)", "Room Code (share with teammates)" },
        ["room_code_join_hint"] = new[] { "队友输入此代码即可加入", "Teammates enter this code to join" },
        ["hide"] = new[] { "隐藏", "Hide" },
        ["subtitle"] = new[] { "外包黑色委托事务所", "Outsourced Commission Office" },
        ["create_office"] = new[] { "创建事务所", "Create Office" },
        ["join_office"] = new[] { "加入事务所", "Join Office" },
        ["create_hint"] = new[] { "创建后邀请队友输入房间代码加入。", "Create and share the room code with teammates." },
        ["hide_direct"] = new[] { "▾ 隐藏直连", "▾ Hide Direct Connect" },
        ["show_direct"] = new[] { "▸ 直连模式 (局域网)", "▸ Direct Connect (LAN)" },
        ["address"] = new[] { "地址", "Addr" },
        ["direct_create"] = new[] { "直连创建", "Direct Host" },
        ["direct_join"] = new[] { "直连加入", "Direct Join" },
        ["enter_code"] = new[] { "输入房间代码", "Enter Room Code" },
        ["ask_host_code"] = new[] { "向房主索取 6 位代码", "Ask host for 6-digit code" },
        ["join"] = new[] { "加入", "Join" },
        ["back"] = new[] { "返回", "Back" },
        ["connecting"] = new[] { "连接中", "Connecting" },
        ["please_wait"] = new[] { "请稍候", "Please wait" },
        ["enter_code_prompt"] = new[] { "请输入房间代码。", "Please enter a room code." },
        ["code_six_chars"] = new[] { "房间代码为 6 位字符。", "Room code is 6 characters." },
        ["relay_unavailable"] = new[] { "连接服务不可用，请使用直连模式。", "Relay unavailable, use direct connect." },
        ["host_started"] = new[] { "主机已启动。", "Host started." },
        ["host_failed"] = new[] { "创建主机失败。", "Failed to create host." },
        ["direct_host_started"] = new[] { "直连主机已启动: {0}:{1}", "Direct host started: {0}:{1}" },
        ["host_port_busy"] = new[] { "创建主机失败: 端口被占用。", "Host failed: port in use." },
        ["port_error"] = new[] { "端口格式错误。", "Invalid port format." },
        ["joining"] = new[] { "正在加入 {0}:{1}...", "Joining {0}:{1}..." },
        ["join_failed"] = new[] { "加入失败。", "Failed to join." },

        // ─── Player identity / lobby roster ───
        ["player_name"] = new[] { "调查员代号", "Agent Name" },
        ["roster_title"] = new[] { "在场队员 {0}/4", "Squad {0}/4" },
        ["you_tag"] = new[] { "(你)", "(you)" },
        ["room_label"] = new[] { "房间码 {0}", "Room {0}" },
        ["lobby_waiting_title"] = new[] { "队伍待命", "Crew Standby" },
        ["lobby_waiting_hint"] = new[] { "队员到齐后进入事务所，在办公室电脑接委托，再去面包车集合。", "Enter the office, accept a commission at the computer, then gather at the van." },
        ["lobby_room_code"] = new[] { "房间码", "Room Code" },
        ["lobby_lan_only"] = new[] { "局域网直连", "LAN Direct" },
        ["lobby_enter_office"] = new[] { "进入事务所", "Enter Office" },
        ["lobby_client_note"] = new[] { "等待房主接单；你可以先进入事务所检查装备。", "Wait for the host to pick a job; you can enter and check gear." },
        ["lobby_empty_slot"] = new[] { "空位 - 等待队友", "Empty - waiting" },

        // ─── Connection events ───
        ["host_disconnected"] = new[] { "主机已断开连接，即将返回...", "Host disconnected. Returning..." },
        ["player_joined"] = new[] { "一名队员已加入", "A teammate joined" },
        ["player_left"] = new[] { "一名队员已离开", "A teammate left" },
        ["you_were_kicked"] = new[] { "你已被房主移出房间。", "You were removed by the host." },
        ["local_mode_only"] = new[] { "联网服务不可用，已切换本地模式（公网无法加入，仅可局域网直连）。", "Online service unavailable: switched to local mode (LAN direct only, no internet join)." },

        // ─── VanTransitOverlay ───
        ["commission"] = new[] { "委托", "Commission" },
        ["mission_location"] = new[] { "任务地点", "Mission Site" },
        ["van_cabin"] = new[] { "委托车后舱", "Van Rear Cabin" },
        ["all_aboard"] = new[] { "全员到齐 {0}/{1}", "All aboard {0}/{1}" },
        ["waiting_team"] = new[] { "等待队友... {0}/{1}", "Waiting... {0}/{1}" },
        ["driver_waiting"] = new[] { "司机在前面等着", "Driver waiting up front" },
        ["press_e_drive"] = new[] { "按 [E] 开走", "Press [E] to drive" },
        ["press_space_depart"] = new[] { "[空格] 发车", "[Space] Depart" },
        ["press_x_leave"] = new[] { "[X] 下车", "[X] Leave seat" },
        ["departing_in"] = new[] { "发车倒计时 {0}…（未上车者将被留下）", "Departing in {0}… (stragglers left behind)" },
        ["wait_all_aboard"] = new[] { "等待所有人上车才能发车", "Everyone must be aboard to depart" },
        ["sit_in_van"] = new[] { "按 [E] 上车就座", "Press [E] to board and sit" },
        ["dispatch_outbound"] = new[] { "派车去现场", "Dispatching to site" },
        ["return_office"] = new[] { "返程回事务所", "Returning to office" },
        ["office"] = new[] { "事务所", "Office" },
        ["in_van"] = new[] { "车内 {0}/4", "In van {0}/4" },

        // ─── MvpHud: Settings ───
        ["pause"] = new[] { "暂停", "Paused" },
        ["resume"] = new[] { "继续", "Resume" },
        ["game"] = new[] { "游戏", "Game" },
        ["language"] = new[] { "语言: {0}", "Language: {0}" },
        ["master_volume"] = new[] { "主音量: {0}", "Master Volume: {0}" },
        ["show_network"] = new[] { "显示网络提示", "Show Network Hints" },
        ["camera"] = new[] { "视角", "Camera" },
        ["h_sensitivity"] = new[] { "鼠标水平速度: {0}", "Horizontal Sensitivity: {0}" },
        ["v_sensitivity"] = new[] { "鼠标垂直速度: {0}", "Vertical Sensitivity: {0}" },
        ["invert_y"] = new[] { "反转垂直视角", "Invert Y-Axis" },
        ["fov"] = new[] { "视野范围: {0}", "Field of View: {0}" },
        ["display"] = new[] { "画面", "Display" },
        ["brightness"] = new[] { "亮度: {0}", "Brightness: {0}" },
        ["fullscreen"] = new[] { "全屏", "Fullscreen" },
        ["quality"] = new[] { "画质: {0}", "Quality: {0}" },
        ["voice"] = new[] { "语音", "Voice" },
        ["voice_default_on"] = new[] { "默认开启语音", "Voice Enabled by Default" },
        ["mute_self"] = new[] { "静音自己", "Mute Self" },
        ["push_to_talk"] = new[] { "按键说话 (按住 V)", "Push-to-Talk (hold V)" },
        ["mic_device"] = new[] { "麦克风: {0}", "Microphone: {0}" },
        ["mic_gain"] = new[] { "麦克风增益: {0}", "Mic Gain: {0}" },
        ["voice_volume"] = new[] { "语音音量: {0}", "Voice Volume: {0}" },
        ["voice_distance"] = new[] { "语音距离: {0} m", "Voice Range: {0} m" },
        ["reset_defaults"] = new[] { "恢复默认", "Reset Defaults" },
        ["quit_game"] = new[] { "退出游戏", "Quit Game" },
        ["prev"] = new[] { "上一个", "Prev" },
        ["next"] = new[] { "下一个", "Next" },

        // ─── MvpHud: Office Panel ───
        ["reward_pending"] = new[] { "结算待领取: {0}。去电脑盖章入账。", "Reward pending: {0}. Claim at computer." },
        ["task_accepted"] = new[] { "委托已接受: {0}。采购后去公司车。", "Commission accepted: {0}. Buy gear, then to van." },
        ["computer_connected"] = new[] { "办公室电脑已连接，可接单或采购。", "Computer online. Accept jobs or buy gear." },
        ["office_idle"] = new[] { "事务所待机中。", "Office on standby." },
        ["terminal_title"] = new[] { "BLACK COMMISSION 委托终端", "BLACK COMMISSION TERMINAL" },
        ["close_computer"] = new[] { "关闭电脑", "Close" },
        ["funds_debt"] = new[] { "资金: {0} G    债务: {1} G", "Funds: {0} G    Debt: {1} G" },
        ["rep_level_xp"] = new[] { "声望: {0}    等级: {1}    经验: {2}/{3}", "Rep: {0}    Level: {1}    XP: {2}/{3}" },
        ["unlocked_categories"] = new[] { "已解锁委托类别: {0}/8", "Unlocked categories: {0}/8" },
        ["takeover_pressure"] = new[] { "被吞并压力: {0}/100", "Takeover Pressure: {0}/100" },
        ["lost_item_progress"] = new[] { "找回失物委托: {0}/2", "Lost Item Jobs: {0}/2" },

        // ─── MvpHud: Warnings ───
        ["hostile_acquired"] = new[] { "警告: 事务所刚被竞对低价吞并，债务重组，等级和委托进度已被压回。", "Warning: Office acquired by competitor. Debt increased, progress reset." },
        ["hostile_acquired_hint"] = new[] { "继续赚钱和提升声望可以降低下一次被吞并风险。", "Earn money and reputation to reduce future takeover risk." },
        ["ultimatum_issued"] = new[] { "竞对发来收购威胁: 事务所已进入最后通牒状态。", "Competitor threat: Office under ultimatum." },
        ["ultimatum_hint"] = new[] { "下一次失败前最好先把资金或声望拉回安全线。", "Raise funds or reputation before the next failure." },
        ["ultimatum_active"] = new[] { "最后通牒: 再失败且资金/声望仍为负，就会被竞对强制吞并。", "Ultimatum: Another failure with negative funds/rep means forced acquisition." },
        ["ultimatum_resolve"] = new[] { "完成委托、攒现金或提高声望可以解除风险。", "Complete commissions or raise funds/rep to lift the threat." },

        // ─── MvpHud: Rewards ───
        ["pending_reward"] = new[] { "待领取奖励: {0}  金钱 {1} / 声望 {2} / 经验 {3}", "Pending: {0}  Money {1} / Rep {2} / XP {3}" },
        ["claim_reward"] = new[] { "领取结算", "Claim Reward" },
        ["wait_host_claim"] = new[] { "等待房主领取结算", "Waiting for host to claim" },
        ["host_only_claim"] = new[] { "结算盖章只能由房主确认，确认后全队同步。", "Only the host can confirm. Team syncs after." },

        // ─── MvpHud: Acquisition ───
        ["acquisition_available"] = new[] { "新手扩张: 可吞并 0 级事务所，费用 {0}G。", "Expansion: Acquire Level 0 office for {0}G." },
        ["confirm_acquire"] = new[] { "确认收购", "Confirm Acquisition" },
        ["acquisition_cost_hint"] = new[] { "需要足够资金，并且被吞并压力低于 70。", "Requires sufficient funds and pressure below 70." },
        ["acquisition_done"] = new[] { "扩张完成: 已吞并一家 0 级事务所，第二类委托入口已解锁为后续内容。", "Expansion complete: Level 0 office acquired. Second category unlocked." },
        ["acquisition_hint"] = new[] { "继续接找回失物任务可以积累资金和声望。", "Keep doing Lost Item jobs to build funds and reputation." },

        // ─── MvpHud: Commission ───
        ["task_locked"] = new[] { "已接受委托: {0}", "Accepted: {0}" },
        ["task_locked_hint"] = new[] { "采购完道具后，去外面的公司车出发。", "Buy gear, then head to the company van." },
        ["available_commissions"] = new[] { "可用委托", "Available Commissions" },
        ["terminal_offline"] = new[] { "委托终端未连接。", "Terminal offline." },
        ["accept_task"] = new[] { "接受委托", "Accept Commission" },
        ["wait_host_accept"] = new[] { "等待房主接受委托", "Waiting for host to accept" },
        ["wait_host_hint"] = new[] { "等待房主选择委托。", "Waiting for host to select." },
        ["start_host_first"] = new[] { "Start Host 后接受委托", "Start Host to accept" },
        ["start_host_hint"] = new[] { "主机启动后可接受委托。", "Start host first." },

        // ─── MvpHud: Shop ───
        ["shop_title"] = new[] { "电脑商店", "Computer Shop" },
        ["claim_first"] = new[] { "先在电脑上领取本次委托结算，之后才能采购下一单道具。", "Claim your pending reward before buying gear." },
        ["shop_hotkeys"] = new[] { "在电脑旁点击购买道具。HQ 内按 G 可把当前热栏道具丢在地上。", "Click to buy items near the computer. Press G to drop items." },
        ["shop_stand_near"] = new[] { "站到电脑前才可采购。在 HQ 内按 G 可把当前热栏道具丢在地上。", "Stand near the computer to buy. Press G anywhere in HQ to drop items." },
        ["insufficient_funds"] = new[] { "资金不足: {0} 需要 {1}G。", "Insufficient funds: {0} costs {1}G." },
        ["cant_store"] = new[] { "{0}无法入库: {1}", "{0} can't be stored: {1}" },
        ["purchase_submitted"] = new[] { "采购申请已提交: {0}，等待账本同步。", "Purchase submitted: {0}, syncing." },
        ["purchase_stamped"] = new[] { "采购申请已盖章: {0} -{1}G。", "Purchase approved: {0} -{1}G." },
        ["purchase_failed"] = new[] { "{0}采购失败。", "{0} purchase failed." },
        ["watch_owned"] = new[] { "你已经戴着一块廉价工时表。", "You already have a cheap wristwatch." },
        ["watch_no_funds"] = new[] { "资金不足: 廉价工时表需要 {0}G。", "Insufficient funds: Wristwatch costs {0}G." },
        ["watch_submitted"] = new[] { "采购申请已提交: 廉价工时表，等待账本同步。", "Purchase submitted: Wristwatch, syncing." },
        ["watch_stamped"] = new[] { "采购申请已盖章: 廉价工时表 -{0}G。", "Purchase approved: Wristwatch -{0}G." },
        ["watch_failed"] = new[] { "廉价工时表采购失败。", "Wristwatch purchase failed." },
        ["watch_owned_label"] = new[] { "廉价工时表 已佩戴", "Wristwatch Equipped" },
        ["watch_buy_label"] = new[] { "廉价工时表  {0}G", "Wristwatch  {0}G" },

        // ─── Item names ───
        ["flashlight"] = new[] { "手电筒", "Flashlight" },
        ["battery"] = new[] { "电池", "Battery" },

        // ─── MvpHud: Mission ───
        ["downed_spectating"] = new[] { "已倒地 — 观察: {0}  [鼠标左键/右键 切换]", "Downed — Spectating: {0}  [LMB/RMB to switch]" },
        ["downed_all"] = new[] { "已倒地 — 全员倒地", "Downed — All players down" },

        // ─── MvpHud: Mission Van ───
        ["mission_van"] = new[] { "委托车后舱", "Van Rear Cabin" },
        ["close_door"] = new[] { "关门", "Close" },
        ["van_decide_hint"] = new[] { "先决定是否返程；补给柜只是可选物资，不影响关门回事务所。", "Decide whether to return first; locker supplies are optional." },
        ["van_locker"] = new[] { "车载补给柜", "Van Locker" },
        ["take_item"] = new[] { "取出", "Take" },
        ["locker_request"] = new[] { "车载物资申请: {0}。", "Requesting: {0}." },
        ["continue_search"] = new[] { "继续搜索", "Continue Search" },
        ["partial_return_warn"] = new[] { "警告: 再次点击返程会让全队提前回事务所，只做部分结算。", "Warning: Clicking return again sends everyone back for partial pay." },

        // ─── PlayerHealth ───
        ["revive_progress"] = new[] { "救援队友 ({0}%)", "Reviving ({0}%)" },
        ["revive_hint"] = new[] { "救援队友", "Revive Teammate" },

        // ─── MvpHud: Field Clock ───
        ["wristwatch_time"] = new[] { "工时表: {0}    合同截止: {1}", "Watch: {0}    Deadline: {1}" },
        ["daylight_label"] = new[] { "天光判断: {0}", "Daylight: {0}" },
        ["no_watch_hint"] = new[] { "准确时间: 回委托车看车载钟，或在事务所购买廉价工时表。", "Check the van clock or buy a wristwatch for exact time." },
        ["overtime_feeling"] = new[] { "你感觉已经拖过合同窗口了，返程结算会被扣。", "You feel you've gone past the contract window. Penalties apply." },
        ["van_clock"] = new[] { "车载时钟: {0}    标准下班: {1}", "Van clock: {0}    End of shift: {1}" },
        ["remaining_window"] = new[] { "剩余窗口: {0}", "Remaining: {0}" },
        ["overtime_penalty"] = new[] { "超时: {0}    预计扣款 -{1}G / 声望 -{2}", "Overtime: {0}    Penalty -{1}G / Rep -{2}" },
        ["overtime_reward_penalty"] = new[] { "含超时扣罚: {0}  -{1}G / 声望 -{2}", "Overtime penalty: {0}  -{1}G / Rep -{2}" },
        ["wristwatch_status_owned"] = new[] { "已佩戴工时表", "Watch equipped" },
        ["wristwatch_status_none"] = new[] { "未购工时表", "No watch" },
        ["hotbar_summary"] = new[] { "热栏: {0}/{1}格  手电x{2} / 电池x{3}  {4}", "Hotbar: {0}/{1}  Flashlight x{2} / Battery x{3}  {4}" },
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
