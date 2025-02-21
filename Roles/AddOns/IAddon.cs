//Thanks EHR for https://github.com/Gurge44/EndlessHostRoles/blob/main/Roles/AddOns/IAddon.cs and everything related ;)

namespace TOHE.Roles.AddOns
{
    [Obfuscation(Exclude = true)]
    public enum AddonTypes
    {
        Impostor,
        Helpful,
        Harmful,
        Misc,
        Guesser,
        Mixed,
        Experimental
    }
    public interface IAddon
    {
        public CustomRoles Role { get; }
        public AddonTypes Type { get; }
        public void SetupCustomOption();

        public void Init();
        public void Add(byte playerId, bool gameIsLoading = true);
        public void Remove(byte playerId);
        public void OnFixedUpdate(PlayerControl pc)
        { }
        public void OnFixedUpdateLowLoad(PlayerControl pc)
        { }
    }
}
