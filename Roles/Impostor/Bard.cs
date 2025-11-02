using System;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Bard : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bard;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static int BardCreations;

    public override void Init()
    {
        BardCreations = 0;
    }

    public static bool CheckSpawn()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(0, 100) < Arrogance.BardChance.GetInt();
    }

    public override void OnPlayerExiled(PlayerControl bard, NetworkedPlayerInfo exiled)
    {
        if (exiled != null) Main.AllPlayerKillCooldown[bard.PlayerId] /= 2;
    }

    public static void OnMeetingHudDestroy(ref string name)
    {
        try
        {
            BardCreations++;

            string json = ModUpdater.Get("https://official-joke-api.appspot.com/random_joke");
            var joke = JsonUtility.FromJson<Joke>(json);
            name = $"{joke.setup}\n{joke.punchline}";

            name += "\n\t\t——" + Translator.GetString("ByBard");
        }
        catch (Exception e)
        {
            Utils.ThrowException(e);
            name = Translator.GetString("ByBardGetFailed");
        }
    }

    [Serializable]
    public class Joke
    {
        public string type;
        public string setup;
        public string punchline;
        public int id;
    }
}
