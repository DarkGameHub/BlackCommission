Phase 1 MVP Development Checklist — "Outsourced Incident Response"

> **Status note**: This document is the early "underground mall flooding incident" prototype checklist, kept as a reference for the old approach. The current playable MVP has shifted to the flow described in `docs/mvp-core-loop.md`: "bankrupt office → office computer → accept school homework-retrieval commission → go directly to the mission site → return to office for settlement." The Phase 1 build does not include a vehicle/parking-lot departure flow.

MVP Goal

Phase 1 delivers one complete, playable Demo:

4 players accept an "underground mall flooding incident" work order from a broke office, enter the incident site, rescue survivors, repair the pump, evade a malfunctioning cleaning robot, collect evidence and evacuate, then return to the office for settlement.

0. Project Foundation
P0 Required
0.1 Create Unity Project
Use Unity 6 LTS or a stable LTS release.
Project type: 3D.
Render pipeline: URP.
Set up the project directory structure:
Assets/
  Art/
  Audio/
  Characters/
  Code/
  Networking/
  Prefabs/
  Scenes/
  ScriptableObjects/
  UI/
  VFX/
0.2 Base Scenes

Create the following scenes:

MainMenu
Office_Hub
Mission_MallFlood
ResultScreen
0.3 Version Control
Git repository.
Set up .gitignore.
Use Git LFS for large files.
Recommended branches:
main
develop
feature/*
bugfix/*
0.4 Base Build Targets
Windows PC.
Support local builds.
Support LAN/Steam pre-testing environment.
1. Networking System

This is one of the most critical technical modules in the MVP.

P0 Required
1.1 Room System

Features:

Create room;
Join room;
Leave room;
Host starts game;
Up to 4 players.

Public matchmaking is not required for Phase 1.

Host Game
Join Game
Ready
Start Mission
Leave Lobby
1.2 Player Synchronization

Sync the following:

Player position;
Player rotation;
Player animation state;
Item held by player;
Whether player is downed;
Whether player has evacuated;
Player's current interaction state.
1.3 Host Authority

The following logic is determined by the Host:

Mission state;
Water level phase;
Cleaning robot AI;
Survivor state;
Item spawning;
Settlement result;
Evacuation determination.
1.4 Disconnect Handling

Simple handling is sufficient for MVP:

On disconnect, player character disappears or becomes downed;
If the host disconnects, mission fails or returns to lobby;
Show disconnect notification.
P1 Recommended
1.5 Simple Lobby Ready State
Each player can click Ready.
Host can only start when at least 1 player is Ready.
UI shows player names and ready state.
1.6 Simple Invite/Join Code
Join via room code.
Or Steam friend invite, depending on the technical solution.
2. Player Controller
P0 Required
2.1 First-Person Movement

Basic controls:

WASD movement;
Mouse look;
Sprint;
Crouch;
Jump;
Interact key;
Drop key;
Use tool key.
2.2 Stamina System

Stamina affects:

Sprinting;
Carrying items;
Lifting survivors;
Pushing heavy objects.

Features:

Sprinting drains stamina;
Stamina recovers after stopping;
Stamina drains faster when carrying heavy items;
Cannot sprint when stamina is at 0.
2.3 Player Injury/Downed State

MVP simple version:

Player takes damage when hit or shocked by the robot;
Health at zero = downed;
Teammates can revive;
Downed player has limited or no movement;
Being downed too long counts as seriously injured.
2.4 Interaction System

Unified interaction framework:

Look at an object to show interaction prompt;
Press E to interact;
Hold E for repairs/rescues/pickups;
Can be interrupted;
Interaction state synced over network.
P1 Recommended
2.5 Stability System

Lightweight version is fine.

Decreases when:

In darkness for a long time;
Chased by the robot;
Near a hazard zone;
A teammate is downed;
Water level enters the out-of-control phase.

Effects:

Slight screen shake;
Louder breathing sound;
Slightly slower repair speed.

Keep it light — avoid making it annoying.

3. Voice System
P0 Required
3.1 Proximity Voice

Features:

Sound gets quieter the farther players are;
Walls can simply reduce volume;
Downed player's voice can be quieter or intermittent.
3.2 Walkie-Talkie Voice

Features:

Push to talk;
Heard by the whole team;
Simple static noise effect;
More obvious static during the out-of-control water phase.
3.3 Voice UI

Shows:

Who is speaking;
Whether walkie-talkie is in use;
Whether walkie-talkie signal is unstable.
P1 Recommended
3.4 Zone Signal Interference

Walkie-talkie audio quality degrades in underground storage and pump rooms.

4. Office Hub
P0 Required
4.1 Office_Hub Scene

Basic layout:

Rundown office;
Work order computer;
Gear rack;
Departure door;
Whiteboard;
Funds/reputation display.
4.2 Work Order Computer

Features:

View current work order;
View mission objectives;
View estimated payment;
View risk level;
Start mission.

MVP only needs one work order:

Underground mall flooding incident
4.3 Gear Selection

Players can choose basic tools from the gear rack.

Phase 1 tools:

Flashlight;
Walkie-talkie;
Toolbox;
Stretcher;
Temporary battery.
4.4 Departure Flow
Everyone ready;
Host confirms departure;
Switch to mission scene;
Sync all players into the level.
P1 Recommended
4.5 Hub Atmosphere Interactions
Ringing phone;
Debt whiteboard on the wall;
Previous mission settlement slip;
Office funds display;
"Zero Incidents: 0 Days" sign.
5. Work Order System
P0 Required
5.1 Work Order Data Structure

Recommended fields:

Work order name
Work order description
Location
Risk level
Base payment
Primary objective list
Optional objective list
Hidden objective list
Failure conditions
Time limit
5.2 Primary Objectives

Primary objectives for the underground mall flooding incident:

1. Rescue at least 1 survivor
2. Restart the drainage pump
3. At least 1 player successfully evacuates
5.3 Optional Objectives

Phase 1 optional objectives:

1. Rescue a 2nd survivor
2. Bring back incident evidence
3. Bring back the safe
4. Complete within 15 minutes
5.4 Mission UI

Displayed in-level:

Current primary objective;
Objective completion status;
Optional objectives;
Evacuation prompt;
Incident escalation warning.
5.5 Mission Completion Determination

Completion conditions:

Primary objective complete;
Player reaches the evacuation point;
Host determines mission success;
Proceed to settlement.

Failure conditions:

All players downed;
Forced lockdown time expires;
Player evacuates without completing primary objective — counts as partial failure;
All survivors die — rescue objective fails.
6. Map: Underground Mall Flooding Incident
P0 Required
6.1 Graybox Map

Map areas:

Entrance hall
Shop corridor
Food court
Kitchen
Staff passage
Electrical control room
Drainage pump room
Underground storage
Parking lot evacuation zone
6.2 Route Structure

Must include:

Loop route;
At least 2 routes to the pump room;
At least 2 routes to the storage room;
One route that gets blocked by water later;
One security door that needs power to open;
One shortcut door that can be unlocked from behind.
6.3 Temporary Safe Zone

Entrance hall serves as a temporary safe zone.

Features:

Safe in the early phase;
Can place survivors here;
Can redistribute gear;
Show mission status;
Not guaranteed to be safe later.
6.4 Evacuation Point

Parking lot evacuation zone.

Features:

Players entering the evacuation zone trigger "waiting to evacuate" display;
Survivors must be brought here to count as rescued;
Can evacuate once mission objectives are met;
Host confirms or countdown ends, then settlement begins.
P1 Recommended
6.5 Map Art Replacement

Priority replacements:

Entrance hall;
Pump room;
Storage room;
Parking lot evacuation zone.
6.6 Dynamic Environment Changes
Rising water visual;
Flickering lights;
Warning lights;
Broadcast announcements;
Access door malfunctions and sparks.
7. Incident Escalation System
P0 Required
7.1 Global Incident Timer

Default for one session:

0–5 minutes: Controlled phase
5–10 minutes: Deteriorating phase
10–15 minutes: Out-of-control phase
After 15 minutes: Forced evacuation phase
7.2 Water Level Phases

At least 4 phases:

Phase 0: No visible flooding
Phase 1: Low water, slight slowdown
Phase 2: Medium water, noticeable slowdown, some electrical hazards
Phase 3: High water, some passages blocked
7.3 Water Level Effects

Affects:

Player movement speed;
Carry speed;
Some doors cannot be opened;
Some electrical zones cause shock damage;
Survivor movement speed;
Cleaning robot path changes.
7.4 Pump Repair Effects

After the drainage pump starts:

Water rise rate decreases;
Some areas become passable again;
Primary objective completed;
New incident events trigger, e.g. robot alert level increases.
7.5 Phase Broadcasts

Each phase triggers a broadcast:

"Drainage system anomaly detected on basement level 2."
"Water level continues to rise. Avoid low-lying areas."
"Parking lot gate will close in 3 minutes."
"Property management thanks you for your cooperation."
P1 Recommended
7.6 Random Incident Events

Simple random events:

A door short-circuits and locks;
Lights go out in an area;
Robot changes patrol route;
Survivor cries out and reveals their position;
A piece of evidence gets washed away.
8. Survivor System
P0 Required
8.1 Survivor Base States

States:

Waiting for rescue
Following player
Being dragged
Being carried
Downed
Evacuated
Dead
8.2 Survivor 1: Lightly Injured

Location: Food court kitchen.

Behavior:

Player can calm them down after finding them;
After being calmed, follows the nearest player;
Panics when the robot is near;
Slows down when water level is high;
Counts as rescued when they reach the evacuation point.
8.3 Survivor 2: Seriously Injured

Location: Underground storage.

Behavior:

Cannot move on their own;
Can be dragged by one person;
Requires two people with a stretcher to carry;
Deteriorates continuously if left in a high water zone;
May be dragged away by the robot if it finds them.
8.4 Survivor Health State

Effects:

Deteriorates if not rescued for a long time;
Deteriorates if hit by the robot;
Deteriorates in high water;
Players can perform simple first aid to slow deterioration.
8.5 Rescue Determination

Survivor enters the evacuation zone:

Status changes to "evacuated";
Updates mission objective;
Settlement adds money and reputation.
P1 Recommended
8.6 Survivor Emotion

Lightweight behaviors:

Cries out in fear;
Stops following;
Hides;
Needs a player to calm them down.
9. Drainage Pump Repair System
P0 Required
9.1 Pump Room Equipment

Pump room contains:

Main control panel;
Fuse slot;
Manual valve;
Temporary battery port.
9.2 Repair Flow

Flow:

Enter pump room
→ Inspect control panel
→ Find fuse
→ Install fuse
→ Install temporary battery or restore power
→ One player operates the control panel
→ One player turns the valve
→ Progress bar fills
→ Drainage pump starts
9.3 Two-Person Cooperation

Requirements:

Control panel and valve must be operated simultaneously within a short window;
If only one person is present, the operation fails;
Being hit by the robot during operation will interrupt it.
9.4 Repair Feedback
Progress bar;
Sound feedback;
Lights restored;
Pump start vibration;
Mission objective completion prompt.
P1 Recommended
9.5 Repair Mini-Game

Lightweight options:

Wire sequence;
Pointer alignment;
Keeping valve pressure within the safe range.
10. Cleaning Robot AI
P0 Required
10.1 Robot Base Behavior

State machine:

Patrol
Investigate sound
Detect target
Charge
Drag item/survivor
Return to patrol
Brief shutdown
10.2 Patrol
Patrols along fixed route;
Passes through shop corridor, food court, and staff passage;
Changes route when water level is high.
10.3 Sound Investigation

Sound sources:

Player sprinting;
Item dropping;
Walkie-talkie;
Survivor crying out;
Failed repair;
Door being knocked open.

Robot heads to the source location after hearing a sound.

10.4 Detect Player

After spotting a player:

Emits a property management announcement;
Brief lock-on;
Charges the player;
Impact deals damage and knockback;
Interrupts carrying/repair.
10.5 Drag Items

Robot attempts to drag away:

Dropped small props;
Evidence boxes;
Downed survivors;
Lightly injured survivors.
10.6 Weakness

Players can temporarily shut down the robot with a flashlight's bright beam.

Limitations:

Short shutdown duration;
Has a cooldown;
Robot resistance increases in later phases.
P1 Recommended
10.7 Robot Voice Lines

Examples:

"Obstacle detected."
"Please keep the mall clean."
"Humanoid waste, please cooperate with recycling."
"Thank you for your understanding."
10.8 Robot Escalation

The higher the incident phase:

Patrol speed increases;
Hearing becomes more sensitive;
Charge cooldown is shorter.
11. Item and Equipment System
P0 Required
11.1 Universal Item System

Items must support:

Pickup;
Drop;
Carry;
Use;
Network sync;
Being dragged away by the robot;
Being damaged by water.
11.2 Flashlight

Features:

Illumination;
Battery charge;
Bright beam temporarily shuts down robot;
Can be toggled.
11.3 Walkie-Talkie

Features:

Long-distance voice communication;
Battery or signal state;
Generates noise risk when in use.
11.4 Toolbox

Features:

Repair doors;
Repair electrical panels;
Repair pump;
Open maintenance panels.
11.5 Stretcher

Features:

Fold/unfold;
Two players carry a seriously injured survivor;
Two players must cooperate when carrying;
Drops when it hits a door or takes impact.
11.6 Temporary Battery

Features:

Powers access doors;
Powers pump room control panel;
Heavy item;
Damaged if dropped in water.
11.7 Fuse

Features:

Required item for pump repair;
Can be found in the electrical control room;
Small item, can be vacuumed up by the robot.
11.8 Evidence Box/Hard Drive

Features:

Optional objective;
Returning it to the evacuation point adds money at settlement;
Damaged by water, reducing its value.
P1 Recommended
11.9 Safe
Heavy item;
Two-person carry;
High value;
Value decreases if dropped or flooded.
12. Carrying System
P0 Required
12.1 Single-Person Carry

Supports:

Hold small items in one hand;
Carry heavy items with both hands;
Drag a seriously injured person.

Effects:

Movement speed reduced;
Some tools cannot be used;
Sprinting limited.
12.2 Two-Person Carry

Supports:

Stretcher;
Seriously injured survivor;
Safe;
Large battery.

Requirements:

Two players interact with the two ends separately;
Movement must be synchronized;
Turning has inertia;
Impact causes a drop;
Getting stuck in doors should produce physical comedy.
12.3 Dropping and Damage

After dropping:

Survivor's condition worsens;
Evidence is damaged;
Safe value decreases;
Sound is generated, attracting the robot.
P1 Recommended
12.4 Simple Physics Feedback
Heavy item hitting a door;
Drag resistance in water;
Stretcher tilting;
Impact sound effects.
13. Doors, Power, and Environment Interaction
P0 Required
13.1 Door System

Door types:

Normal door;
Access-controlled door;
Shortcut door;
Door jammed by water pressure;
Door requiring a toolbox to repair.
13.2 Power System

States:

Normal;
Unstable;
Power outage.

Effects:

Lighting;
Access doors;
Walkie-talkie signal;
Pump room control panel;
Electrocution zones.
13.3 Electrical Control Room

Features:

Can restore power to some areas;
Requires a toolbox;
May trigger robot alert.
P1 Recommended
13.4 Electrocution Hazard
Electrified zones in water;
Player takes damage on entry;
Can be disarmed by cutting the breaker.
14. Evacuation System
P0 Required
14.1 Evacuation Zone

Parking lot evacuation point.

Features:

Players entering the zone triggers "evacuating" status;
Survivors entering the zone are recorded;
Can evacuate once mission objectives are met.
14.2 Evacuation Determination

Supports:

Full team evacuation;
Partial player evacuation;
Player downed without evacuating;
Survivor evacuation;
Item evacuation.
14.3 Forced Evacuation Countdown

Starts after 15 minutes:

Countdown displayed;
Parking lot gate about to close;
Characters not evacuated are treated as missing/seriously injured.
15. Settlement System
P0 Required
15.1 Settlement Data Collection

Records:

Whether primary objective was completed;
Number of survivors rescued;
Number of evidence items returned;
Whether the safe was returned;
Player injury status;
Survivor death status;
Equipment damage;
Property damage;
Completion time.
15.2 Income Calculation

Income items:

Primary objective completion bonus
Survivor rescue bonus
Pump repair bonus
Evidence bonus
Safe recovery bonus
Speed bonus
15.3 Deduction Calculation

Deduction items:

Team member medical fees
Survivor death compensation
Equipment damage fees
Client property damage fees
Overtime penalty
Lost tool fees
15.4 Net Profit

Calculation:

Net profit = total income - total deductions
Office funds += net profit
15.5 Reputation Changes

Increases:

Complete primary objective;
Rescue survivors;
Finish quickly;
Return evidence.

Decreases:

Mission failure;
Survivor death;
Team member seriously injured;
Evidence damaged;
Severe property damage.
15.6 Settlement Screen

Displays:

Mission result
Number rescued
Completion time
Income breakdown
Deduction breakdown
Net profit
Reputation change
Office current funds
Team member status
P1 Recommended
15.7 Humorous Settlement Rating

Examples:

"Barely avoided bankruptcy"
"Client says they won't be hiring you again"
"Rescue successful, but property management is furious"
"Net profit this run: 37 credits. Worth celebrating"
16. UI System
P0 Required
16.1 Main Menu UI
Start game;
Create room;
Join room;
Settings;
Quit.
16.2 Hub UI
Player list;
Current funds;
Current reputation;
Work order details;
Gear selection;
Ready status;
Depart button.
16.3 In-Level UI
Primary objective;
Optional objectives;
Teammate status;
Currently carried item;
Stamina bar;
Water level warning;
Incident phase;
Evacuation direction;
Interaction prompts.
16.4 Settlement UI
Mission completion status;
Income;
Deductions;
Reputation;
Return to office button.
P1 Recommended
16.5 Mission Prompts

Show on first encounter:

How to rescue a person;
How to repair the pump;
How to use the stretcher;
How to evacuate.
17. Audio System
P0 Required
17.1 Ambient Audio
Water dripping;
Pooled water flowing;
Distant broadcast;
Mall air conditioning noise;
Electrical short circuit;
Robot movement sounds.
17.2 Interaction Audio
Opening a door;
Picking up;
Dropping;
Repairing;
Turning a valve;
Pump starting;
Installing a battery;
Installing a fuse.
17.3 Danger Audio
Water level alarm;
Robot lock-on;
Robot charging;
Survivor calling for help;
Countdown broadcast;
Walkie-talkie static.
17.4 Settlement Audio
Income sound effect;
Deduction sound effect;
Reputation change sound effect.
P1 Recommended
17.5 Robot Voice Line Library

At least 10 lines.

17.6 Office Ambient Audio
Old fan;
Phone ringing;
Printer paper jam;
Upstairs water leak.
18. Art Asset List
P0 Required
18.1 Player Characters
4 color-coded low-cost uniform characters;
First-person hands;
Simple third-person model;
Base animations:
Idle
Walk
Run
Crouch
Pick up
Carry
Downed
Revive
Repair
18.2 Map Graybox Assets
Walls;
Floors;
Doors;
Stairs/ramps;
Shop storefronts;
Pump room equipment;
Control panel;
Storage shelves;
Parking lot gate.
18.3 Key Props
Flashlight;
Walkie-talkie;
Toolbox;
Stretcher;
Temporary battery;
Fuse;
Evidence hard drive;
Evidence box;
Safe.
18.4 Survivors
Lightly injured survivor model;
Seriously injured survivor model;
Simple animations:
Sitting and waiting
Standing up
Following
Panicking
Downed
Being carried
18.5 Cleaning Robot
One main robot model;
Patrol animation;
Charge animation;
Drag animation;
Short-circuit shutdown animation.
P1 Recommended
18.6 Atmosphere Art
Hazard tape;
Wet floor reflections;
Mall signage;
Damaged advertising boards;
Floating debris;
Garbage bags;
Electrical sparks in water;
Property management notice boards.
19. VFX System
P0 Required
Water surface effect;
Water splashes;
Electrical sparks;
Light flickering;
Robot impact sparks;
Repair completion effect;
Pump start effect.
P1 Recommended
Water level rise visual transition;
Electrocution effect;
Walkie-talkie interference screen effect;
Robot brief shutdown smoke effect.
20. Tutorial and Guidance
P0 Required
20.1 Hub Introduction

Work order computer instructions:

Objective: Rescue survivors, repair the drainage pump, then evacuate.
Water level rises over time.
Waiting too long loses money; too many deaths also loses money.
20.2 In-Level Prompts

Show on first trigger:

Find the survivor;
Need to carry/escort them;
Find the pump room;
Need a fuse;
Requires two people to operate;
You can evacuate.
P1 Recommended
20.3 Training Corner

Place a small training area in the office:

Practice carrying a stretcher;
Practice picking up items;
Practice repairs.
21. Test Checklist
P0 Must Test
21.1 Network Stability

Test items:

4 players join;
Host starts mission;
A player disconnects mid-mission;
Multiple players pick up items simultaneously;
Multiple players interact with pump room simultaneously;
Multiple players carry a stretcher;
Multiple players evacuate.
21.2 Core Flow

Must complete end-to-end:

Accept work order in hub
→ Select gear
→ Enter map
→ Find survivor
→ Repair drainage pump
→ Evacuate
→ Settlement
→ Return to hub
21.3 Mission Determination

Test:

Rescuing only 1 person — does it complete;
Rescuing 2 people — does it add money;
Not repairing pump — can mission complete;
How settlement works when a player doesn't evacuate;
How deductions work when a survivor dies;
How settlement works when evidence is damaged.
21.4 Water Level System

Test:

Water rises over time;
Correct change after pump repair;
Water level affects movement;
Water level blocks passages;
Water level affects survivors.
21.5 Cleaning Robot

Test:

Patrol works normally;
Sound investigation works;
Robot charges player;
Interrupts carrying;
Drags away items;
Can be temporarily shut down by flashlight.
22. Balance Goals
P0 Balance Standards
Session Duration

Target:

Experienced players: complete in 8–12 minutes;
Average players: complete in 12–18 minutes;
Greedy players: 18+ minutes at high risk.
Mission Difficulty

Phase 1 target:

May fail on first attempt;
Understand the flow by the second or third run;
After getting good, can attempt rescuing 2 survivors and grabbing evidence;
Players should frequently debate "should we grab one more thing?"
Economy

Target:

Completing primary objective makes a small profit;
Completing optional objectives makes a large profit;
Failure results in a loss;
Injuries/damage significantly impacts profit.
23. MVP Acceptance Criteria

The Demo succeeds not by how much content it has, but by whether these moments occur naturally.

Must Occur

Players should naturally say:

"You go repair the pump, I'll find the person."
"The stretcher is stuck!"
"The robot dragged the person away!"
"Water's rising, let's evacuate quickly."
"Should we grab that safe on the way out?"
"How did we lose money again this run?"
If these don't happen, the design has failed

Failure Signals:

Players don't know the objectives;
Players don't need to communicate;
Players always stick together;
Players have no division of labor;
Time pressure isn't noticeable;
The robot is just annoying, not funny;
Carrying isn't fun;
Settlement has no emotional payoff.
24. Recommended Development Order
Step 1: Networked Graybox

Prioritize:

4 players in room
First-person movement
Basic interactions
Graybox map
Step 2: Complete Mission Loop

Build:

Work order objectives
Survivor
Pump repair
Evacuation
Settlement
Step 3: Incident Escalation

Build:

Water level rising
Lighting changes
Broadcast countdown
Route blockage
Step 4: Chaos Source

Build:

Cleaning robot
Impact
Dragging items
Sound investigation
Step 5: Carrying Fun

Build:

Stretcher
Two-person carry
Heavy items
Drop/damage
Step 6: Office Wrapper

Build:

Hub
Work order computer
Gear rack
Funds and reputation
Return to hub
Step 7: Audio and Art

Build:

Key sound effects
Robot voice lines
Water level visuals
Map art replacement
UI polish
25. Phase 1 Final Deliverables

At the end of Phase 1, the development team should deliver:

1. A Windows Demo supporting 4-player online co-op
2. One office hub
3. One complete work order: underground mall flooding incident
4. One playable underground mall map
5. Rescue system
6. Drainage pump repair system
7. Water level incident escalation system
8. Cleaning robot
9. Carrying system
10. Evacuation system
11. Settlement system
12. Basic sound effects and UI
13. Supports at least 3 consecutive playable sessions
26. Minimum Viable Version Definition

The true minimum viable version can be compressed to this:

4-player online
→ Accept work order from office
→ Enter underground mall
→ Find 1 survivor
→ Find fuse
→ Repair drainage pump
→ Evade cleaning robot
→ Bring survivor to evacuation
→ Settlement: profit or loss
→ Return to office

Once this loop runs end-to-end, you can start testing with friends.
