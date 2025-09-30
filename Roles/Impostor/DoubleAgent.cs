using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;
internal class DoubleAgent : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.DoubleAgent;
    private const int Id = 29000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\
    private static readonly List<GameObject> createdButtonsList = [];
    private static readonly HashSet<byte> CurrentBombedPlayers = [];
    private static float CurrentBombedTime;
    public static bool BombIsActive = false;
    public static bool CanBombInMeeting = true;
    public static bool StartedWithMoreThanOneImp = false;

    public static OptionItem DoubleAgentCanDiffuseBombs;
    private static OptionItem ClearBombedOnMeetingCall;
    private static OptionItem CanUseAbilityInCalledMeeting;
    private static OptionItem BombExplosionTimer;
    private static OptionItem ExplosionRadius;
    private static OptionItem ChangeRoleToOnLast;

    [Obfuscation(Exclude = true)]
    public static readonly string[] CRoleChangeRolesString =
    [
        GetString("Role_NoChange"),
        GetString("Role_Random"),
        $"{CustomRoles.Admired.ToColoredString()} {CustomRoles.Impostor.ToColoredString()}",
        CustomRoles.Traitor.ToColoredString(),
        CustomRoles.Trickster.ToColoredString(),
    ];
    public static readonly CustomRoles[] CRoleChangeRoles =
    [
        0, // NoChange
        0, // Random
        CustomRoles.ImpostorTOHE, // Team Crewmate
        CustomRoles.Traitor, // Team Neutral
        CustomRoles.Trickster, // Team Impostor as Crewmate
    ];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.DoubleAgent);
        DoubleAgentCanDiffuseBombs = BooleanOptionItem.Create(Id + 10, "DoubleAgentCanDiffuseBombs", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent]);
        ClearBombedOnMeetingCall = BooleanOptionItem.Create(Id + 11, "DoubleAgentClearBombOnMeetingCall", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent]);
        CanUseAbilityInCalledMeeting = BooleanOptionItem.Create(Id + 12, "DoubleAgentCanUseAbilityInCalledMeeting", false, TabGroup.ImpostorRoles, false).SetParent(ClearBombedOnMeetingCall);
        BombExplosionTimer = FloatOptionItem.Create(Id + 13, "DoubleAgentBombExplosionTimer", new(10f, 60f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent])
            .SetValueFormat(OptionFormat.Seconds);
        ExplosionRadius = FloatOptionItem.Create(Id + 14, "DoubleAgentExplosionRadius", new(0.5f, 2f, 0.1f), 1.0f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent])
            .SetValueFormat(OptionFormat.Multiplier);
        ChangeRoleToOnLast = StringOptionItem.Create(Id + 15, "DoubleAgentChangeRoleTo", CRoleChangeRolesString, 1, TabGroup.ImpostorRoles, false, useGetString: false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleAgent]);
    }
    public override void Init()
    {
        CurrentBombedPlayers.Clear();
        CurrentBombedTime = -1;
        BombIsActive = false;
        StartedWithMoreThanOneImp = false;
        CanBombInMeeting = true;
    }

    public override void Add(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        if (Main.AllAlivePlayerControls.Count(player => player.Is(Custom_Team.Impostor)) > 1)
            StartedWithMoreThanOneImp = true;
    }

    private void ClearBomb()
    {
        CurrentBombedPlayers.Clear();
        CurrentBombedTime = -1;
        BombIsActive = false;
        SendRPC();
    }

    // On vent diffuse Bastion & Agitator Bomb if DoubleAgentCanDiffuseBombs is enabled.
    // Dev Note: Add role check for OnCoEnterVentOthers and make BombedVents public in Bastion.cs.
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (DoubleAgentCanDiffuseBombs.GetBool())
        {
            if (pc.PlayerId == Agitater.CurrentBombedPlayer)
            {
                Agitater.ResetBomb();
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                _ = new LateTask(() =>
                {
                    if (pc.inVent) pc.MyPhysics.RpcBootFromVent(vent.Id);
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.DoubleAgent), GetString("DoubleAgent_DiffusedAgitaterBomb")));
                }, 0.8f, "Boot Player from vent: " + vent.Id);
                return;
            }

            var bastion = Main.AllPlayerControls.FirstOrDefault(p => pc.Is(CustomRoles.Bastion));
            if (bastion.GetRoleClass() is Bastion bastionClass && bastionClass.BombedVents.Contains(vent.Id))
            {
                bastionClass.BombedVents.Remove(vent.Id);
                _ = new LateTask(() =>
                {
                    if (pc.inVent) pc.MyPhysics.RpcBootFromVent(vent.Id);
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.DoubleAgent), GetString("DoubleAgent_DiffusedBastionBomb")));
                }, 0.5f, "Boot Player from vent: " + vent.Id);
            }
        }
    }

    public override bool CanUseKillButton(PlayerControl pc) => Main.AliveImpostorCount < 2;

    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (voter.IsModded() || !CanBombInMeeting) return true;

        if (!BombIsActive)
        {
            if (target.Is(Custom_Team.Impostor) && !voter.Is(CustomRoles.Narc)) return false;
            if (target.IsPolice() && voter.Is(CustomRoles.Narc)) return false;
            if (voter == target) return false;

            if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
            {
                target = GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());
                SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
            }

            CurrentBombedTime = -1;
            CurrentBombedPlayers.Add(target.PlayerId);
            BombIsActive = true;
            SendMessage(GetString("VoteHasReturned"), voter.PlayerId, title: ColorString(GetRoleColor(CustomRoles.DoubleAgent), string.Format(GetString("VoteAbilityUsed"), GetString("DoubleAgent"))), noReplay: true);
            return false;
        }
        return true;
    }

    // Clear active bombed players on meeting call if ClearBombedOnMeetingCall is enabled.
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (_Player != null && _Player.IsModded())
        {
            HasVoted = true;
        }

        if (BombIsActive && ClearBombedOnMeetingCall.GetBool())
        {
            ClearBomb();
            if (ClearBombedOnMeetingCall.GetBool() && !CanUseAbilityInCalledMeeting.GetBool()) CanBombInMeeting = false;
        }
        else
            CurrentBombedTime = -1;
    }

    // If bomb is active set timer after meeting.
    public override void AfterMeetingTasks()
    {
        CurrentBombedTime = BombExplosionTimer.GetFloat() + 1f;
        CanBombInMeeting = true;
        SendRPC();
    }

    // Active bomb timer update and check.
    private void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (!CurrentBombedPlayers.Contains(player.PlayerId)) return;

        if (!player.IsAlive()) // If Player is dead clear bomb.
            ClearBomb();

        if (BombIsActive && !GameStates.IsExilling)
        {
            var OldCurrentBombedTime = (int)CurrentBombedTime;

            CurrentBombedTime -= Time.deltaTime;

            if (OldCurrentBombedTime > (int)CurrentBombedTime && CurrentBombedTime < (BombExplosionTimer.GetFloat() + 1))
                SendRPC();

            if (CurrentBombedTime < 1)
                BoomBoom(player);
        }
    }

    // Set timer on Double Agent for Non-Modded Clients.
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        if (BombIsActive)
        {
            if (!player.IsModded())
            {
                string Duration = ColorString(player.GetRoleColor(), string.Format(GetString("DoubleAgent_BombExplodesIn"), (int)CurrentBombedTime));
                if ((!NameNotifyManager.Notice.TryGetValue(player.PlayerId, out var a) || a.Text != Duration) && Duration != string.Empty) player.Notify(Duration, 1.1f);
            }

            if (CurrentBombedPlayers.Any(playerId => !GetPlayerById(playerId).IsAlive())) // If playerId is a null Player clear bomb.
                ClearBomb();
        }

        // If enabled and if DoubleAgent is last Impostor become set role.
        if (ChangeRoleToOnLast.GetValue() != 0 && StartedWithMoreThanOneImp && !GameStates.IsExilling && !AntiBlackout.SkipTasks)
        {
            if (player.Is(CustomRoles.DoubleAgent) && player.IsAlive() && Main.AliveImpostorCount < 2)
            {
                var Role = CRoleChangeRoles[ChangeRoleToOnLast.GetValue()];
                if (ChangeRoleToOnLast.GetValue() == 1) // Random
                    Role = CRoleChangeRoles[IRandom.Instance.Next(2, CRoleChangeRoles.Length)];

                // If role is not on Impostor team remove all Impostor addons if any.
                if (!Role.IsImpostorTeamV3())
                {
                    foreach (CustomRoles allAddons in player.GetCustomSubRoles())
                    {
                        if (allAddons.IsImpOnlyAddon())
                        {
                            Main.PlayerStates[player.PlayerId].RemoveSubRole(allAddons);
                        }
                    }
                }
                // If Role is ImpostorTOHE aka Admired Impostor opt give Admired Addon if player dose not already have it.
                if (Role == CustomRoles.ImpostorTOHE && !player.Is(CustomRoles.Admired) && !player.Is(CustomRoles.Narc))
                    player.GetCustomSubRoles()?.Add(CustomRoles.Admired);

                // If Double Agent is Narc and Role is Traitor,Double Agent turns into Parasite instead
                if (Role is CustomRoles.Traitor && player.Is(CustomRoles.Narc)) Role = CustomRoles.Parasite;

                Init();
                player.GetRoleClass().OnRemove(player.PlayerId);
                player.RpcChangeRoleBasis(Role);
                player.RpcSetCustomRole(Role);
                player.GetRoleClass()?.Add(player.PlayerId);
                player.MarkDirtySettings();

                string RoleName = ColorString(GetRoleColor(player.GetCustomRole()), GetRoleName(player.GetCustomRole()));
                if (Role == CustomRoles.ImpostorTOHE && !player.Is(CustomRoles.Narc))
                    RoleName = ColorString(GetRoleColor(CustomRoles.Admired), $"{GetString("Admired-")}{GetString("ImpostorTOHE")}");
                if (player.Is(CustomRoles.Narc))
                    RoleName = ColorString(GetRoleColor(CustomRoles.Narc), $"{GetString("Narc-")}{GetString(Role.ToString())}");
                player.Notify(ColorString(GetRoleColor(player.GetCustomRole()), GetString("DoubleAgentRoleChange") + RoleName));
            }
        }
    }

    // Players go bye bye ¯\_(ツ)_/¯
    private void BoomBoom(PlayerControl player)
    {
        if (player.inVent) player.MyPhysics.RpcBootFromVent(player.GetPlayerVentId());

        foreach (PlayerControl target in Main.AllAlivePlayerControls) // Get players in radius of bomb that are not in a vent.
        {
            if (GetDistance(player.GetCustomPosition(), target.GetCustomPosition()) <= ExplosionRadius.GetFloat() && !(player.IsTransformedNeutralApocalypse() || target.IsTransformedNeutralApocalypse()))
            {
                if (player.inVent) continue;
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                target.RpcMurderPlayer(target);
                target.SetRealKiller(player);
            }
        }

        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
        ClearBomb();

        _Player?.Notify(ColorString(GetRoleColor(CustomRoles.DoubleAgent), GetString("DoubleAgent_BombExploded")));
    }

    // Set bomb mark on player.
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seen == null) return string.Empty;
        if (CurrentBombedPlayers.Contains(seen.PlayerId)) return ColorString(Color.red, "Ⓑ"); // L Rizz :)
        return string.Empty;
    }


    // Set timer for Double Agent Modded Clients.
    public override string GetLowerText(PlayerControl player, PlayerControl seen, bool isForMeeting = false, bool isForHud = false)
    {
        if (player == null || player != seen || player.IsModded() && !isForHud) return string.Empty;
        if (CurrentBombedTime > 0 && CurrentBombedTime < BombExplosionTimer.GetFloat() + 1) return ColorString(player.GetRoleColor(), string.Format(GetString("DoubleAgent_BombExplodesIn"), (int)CurrentBombedTime));
        return string.Empty;
    }

    // Send bomb timer to Modded Clients when active.
    private void SendRPC(bool addData = false, byte targetId = byte.MaxValue)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(addData);
        writer.Write(targetId);
        writer.WritePacked((int)CurrentBombedTime);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    // Receive and set bomb timer from Host when active.
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        bool addData = reader.ReadBoolean();
        byte targetId = reader.ReadByte();
        var timer = reader.ReadPackedInt32();

        if (addData)
            CurrentBombedPlayers.Add(targetId);

        CurrentBombedTime = timer;
    }

    // Use button for Modded!
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.DoubleAgent) && PlayerControl.LocalPlayer.IsAlive() && CanBombInMeeting && !BombIsActive)
                CreatePlantBombButton(__instance);
        }
    }
    public static void CreatePlantBombButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;
            if (pc.GetCustomRole().GetCustomRoleTeam() == Custom_Team.Impostor || PlayerControl.LocalPlayer == pc) continue;
            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "PlantBombButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            createdButtonsList.Add(targetBox);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("PocketBomb");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => DestroyButtons(targetBox)));
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => PlantBombOnClick(pva.TargetPlayerId /*, __instance*/)));
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => CustomSoundsManager.Play("Line")));
        }
    }

    private static void PlantBombOnClick(byte targetId /*, MeetingHud __instance*/)
    {
        if (BombIsActive) return;

        CurrentBombedTime = -1;
        CurrentBombedPlayers.Add(targetId);
        BombIsActive = true;
    }

    private static void DestroyButtons(GameObject pressedButton)
    {
        foreach (var button in createdButtonsList.Where(button => button != pressedButton))
            UnityEngine.Object.Destroy(button);
        createdButtonsList.Clear();

        pressedButton.GetComponent<PassiveButton>().enabled = false;
        Transform highlightTransform = pressedButton.transform.Find("ControllerHighlight");
        GameObject highlightObject = highlightTransform?.gameObject;
        highlightObject?.SetActive(false);
    }
}
// FieryFlower was here ඞ
// Drakos wasn't here, 100% not
// Niko is here, what dog shxt has you guys code
