# BOT main quest: current integration and limits

StartupBots.xml enables MainQuestsEnabled=true and FirstMainQuestId=100.
Only startup BOTs opt in; the existing GM AddBot command retains its default behavior.
BotMainQuestActions reads loaded MainTaskManager templates from KT_Task/SystemTasks.xml.

Implemented:
- Level, sex, faction, previous-task and already-completed checks.
- Movement through the existing BOT storyboard/pathfinding to a walkable NPC approach.
- NPC proximity checks before accept/objective/submit (100 world units).
- Talk and AnswerQuest objectives. Answers use the same # marker as MainTaskManager.
- Existing TaskManager.IsQuestComplete threshold for those objectives.
- Following NextTask automatically, once per task, with observable stop on unsupported types.
- GM command BotQuestStatus <botId> (send through existing GM command UI/protocol).
- [BotMainQuest] console logs include BOT runtime ID, quest and state/errors.

Not a complete player quest integration:
- BOT progress is in memory, separate from KPlayer TaskDataList/OldTasks and database.
- Rewards are NOT granted. PendingRewardQuestIds records completed objective IDs only;
  it is not an inventory or a durable reward queue. Restart loses these records.
- No XP/level progression, item usage/trading/crafting/enhancing, combat quests,
  cross-map routing, quest Lua callbacks or player completion events.
- NPC objectives resolve by template ID on the current map, as the existing player
  Talk/AnswerQuest progress handler does. They do not teleport to TargetMapCode.

With current XML and village NPC definitions, supported opening sequence is 100..111.
112 (UseSomething / Dot Phao) stops with an unsupported-action error; it is not skipped.
Do not describe this build as supporting the entire main storyline.

Verification:
- Full GameServer compile into _codex_verify_startupbots, without replacing running server.
- MainQuestTests uses actual SystemTasks.xml and village npcs.xml with stub movement/server
  services. Confirms 100..111, stop at 112, pending rewards, prerequisite/answer/proximity rules.
- This does not verify live pathfinding, client visibility, gameplay or player reward/database integration.

Deployment: build/copy updated GameServer.exe and StartupBots.xml to the server installation
when stopped. No server restart or process termination is performed by this change.

## KillMonster integration

BOT-only kill quests now select the first eligible TargetType=1 task (not the configured
FirstMainQuestId). The existing modified BOT eligibility permits independent kill quests;
player prerequisite rules are unchanged. Each new quest resets displayed progress.

BotMainQuestCombat uses loaded map portals to route to and from live monster spawn maps.
Positive TargetMapCode restricts the target map. Special/non-positive codes (including -6)
use a BOT-specific fallback: nearest portal-reachable map containing TargetNPC. This is
not an implementation of every player-side special-map alias. Only loaded world maps and
ordinary configured portals are supported. No route/blocked portal stops with a reason.

Combat uses existing KTSkillManager via the basic skill matching the BOT weapon category,
level 1, at most one attempt/second plus existing skill cooldown checks. Startup quest BOTs
receive an initial +100 physical damage and +1000 attack rating baseline; these are not
progressive equipment stats. Real KTBot.OnKillObject events enqueue kill credit. The BOT
scheduler validates target, map and copy, and stops combat at TargetNum before returning
home to the quest NPC. No kill credit from polling dead monsters.

Rewards and persistence remain pending as documented above. No automated revival is added.
Tests: extracted production FightQuest method with stub combat services verifies wrong
monster/map, no polling credit, duplicate deaths, respawns, skill dispatch and count 5.
Configured portal BFS verifies a route 5 -> 42 to monster 2204. Live combat/pathfinding has
not been verified. Build output is _codex_verify_startupbots/GameServer.exe.
