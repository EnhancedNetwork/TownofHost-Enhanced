using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Eavesdropper : IAddon
{
    public CustomRoles Role => CustomRoles.Eavesdropper;
    public const int Id = 30100;
    private static readonly HashSet<byte> playerList = [];
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem EavesdropPercentChance;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Eavesdropper, canSetNum: true, teamSpawnOptions: true);
        EavesdropPercentChance = FloatOptionItem.Create(Id + 10, "EavesdropPercentChance", new(5f, 100f, 5f), 50f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eavesdropper])
            .SetValueFormat(OptionFormat.Percent);
    }

    public void Init()
    {
        IsEnable = false;
        playerList.Clear();
    }

    public void Add(byte playerId, bool gameIsLoading = true)
    {
        playerList.Add(playerId);
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        playerList.Remove(playerId);

        if (!playerList.Any())
            IsEnable = false;
    }
    public static void GetMessage()
    {
        foreach (var eavesdropperId in playerList)
        {
            if (IRandom.Instance.Next(0, 100) < EavesdropPercentChance.GetFloat())
            {
                // Get all specific msg
                var eavesdropperMsg = MeetingHudStartPatch.msgToSend.Where(x => x.Item2 != 255).Select(x => x.Item1).ToList();

                // Check any data
                if (eavesdropperMsg.Any())
                {
                    // Get random message and send Eavesdropper
                    var randomMsg = eavesdropperMsg.RandomElement();
                    MeetingHudStartPatch.AddMsg(randomMsg, eavesdropperId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eavesdropper), Translator.GetString("EavesdropperMsgTitle")));
                }
            }
        }
    }
}
