using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using System;
using TOHE.Modules.Rpc;
using UnityEngine;


// Credit: https://github.com/Rabek009/MoreGamemodes/blob/e054eb498094dfca0a365fc6b6fea8d17f9974d7/Modules/CustomObjects, https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/CustomNetObject.cs
// Huge thanks to Rabek009 for this code! And thanks to Gurge for some modifications

// 8x8 + Animator : https://ultradragon005.github.io/AmongUs-Utilities/animator.html
// 10x10 : https://ultradragon005.github.io/AmongUs-Utilities/10xeditor.html
// For special grid such as "8x6" jsut copy 10x10 code and ask gpt to make u that specific grid.

//Sidenote: 8x8 on 100% size is a pretty golden standard and trying to make something smaller than that is very ugly (as the grean bean is very visible) so I wouldn't recommend it. 

namespace TOHE.Modules
{
    internal class CustomNetObject
    {
        public static readonly List<CustomNetObject> AllObjects = [];
        private static int MaxId = -1;
        protected int Id;
        protected byte OwnerId;
        public PlayerControl playerControl;
        private float PlayerControlTimer;
        public Vector2 Position;
        public HashSet<byte> HiddenList = [];

        private string Sprite;

        protected void RpcChangeSprite(string sprite)
        {
            Sprite = sprite;
            _ = new LateTask(() =>
            {
                NetworkedPlayerInfo subPlayerInfo = UnityEngine.Object.Instantiate<NetworkedPlayerInfo>(PlayerControl.LocalPlayer.Data);
                subPlayerInfo.NetId = PlayerControl.LocalPlayer.Data.NetId;
                subPlayerInfo.ClientId = PlayerControl.LocalPlayer.Data.ClientId;
                subPlayerInfo.PlayerId = PlayerControl.LocalPlayer.Data.PlayerId;
                subPlayerInfo.name = "CNO_dummy";
                subPlayerInfo.Outfits.Clear();
                subPlayerInfo.FriendCode = PlayerControl.LocalPlayer.Data.FriendCode;
                subPlayerInfo.Puid = GameStates.IsVanillaServer ? PlayerControl.LocalPlayer.Data.Puid : "";
                subPlayerInfo.PlayerLevel = 250;
                subPlayerInfo.Tasks.Clear();
                subPlayerInfo.Role = PlayerControl.LocalPlayer.Data.Role;
                subPlayerInfo.DespawnOnDestroy = false;

                NetworkedPlayerInfo.PlayerOutfit playerOutfit = new();
                playerOutfit.Set("<size=14><br></size>" + sprite, 255, "", "", "", "", "");

                playerControl.RawSetName(sprite);
                subPlayerInfo.Outfits[PlayerOutfitType.Default] = playerOutfit;

                var sender = CustomRpcSender.Create("SetFakeData");
                MessageWriter writer = sender.stream;
                sender.StartMessage();

                writer.StartMessage(1);
                {
                    writer.WritePacked(playerControl.NetId);
                    writer.Write(PlayerControl.LocalPlayer.PlayerId);
                }
                writer.EndMessage();

                writer.StartMessage(1);
                {
                    writer.WritePacked(subPlayerInfo.NetId);
                    subPlayerInfo.Serialize(writer, false);
                }
                writer.EndMessage();

                sender.StartRpc(playerControl.NetId, (byte)RpcCalls.Shapeshift)
                    .WriteNetObject(PlayerControl.LocalPlayer)
                    .Write(false)
                    .EndRpc();

                writer.StartMessage(1);
                {
                    writer.WritePacked(playerControl.NetId);
                    writer.Write((byte)255);
                }
                writer.EndMessage();

                writer.StartMessage(1);
                {
                    writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                    PlayerControl.LocalPlayer.Data.Serialize(writer, false);
                }

                writer.EndMessage();
                sender.EndMessage();
                sender.SendMessage();

                UnityEngine.Object.Destroy(subPlayerInfo.gameObject);
            }, 0f, "CNO_RpcChangeSprite");
        }

        public void TP(Vector2 position)
        {
            playerControl.NetTransform.RpcSnapTo(position);
            Position = position;
        }

        public void Despawn()
        {
            Logger.Info($" Despawning Custom Net Object {GetType().Name} (ID {Id})", "CNO.Despawn");

            try
            {
                playerControl.Despawn();
                AllObjects.Remove(this);
            }
            catch (Exception e)
            {
                Utils.ThrowException(e);
            }
        }

        protected void Hide(PlayerControl player)
        {
            Logger.Info($" Hide Custom Net Object {GetType().Name} (ID {Id}) from {player.GetNameWithRole()}", "CNO.Hide");

            HiddenList.Add(player.PlayerId);
            if (player.AmOwner)
            {
                _ = new LateTask(() =>
                {
                    playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(false);
                }, 0.1f, "Hide NameText_TMP", shoudLog: false);
                playerControl.Visible = false;
                return;
            }

            _ = new LateTask(() =>
            {
                CustomRpcSender sender = CustomRpcSender.Create("FixModdedClientCNOText", sendOption: SendOption.Reliable);
                sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixModdedClientCNO, player.GetClientId())
                    .WriteNetObject(playerControl)
                    .Write(false)
                    .EndRpc();
                sender.SendMessage();
            }, 0.4f, "Send RPC FixModdedClientCNOText", shoudLog: false);

            var message = new DespawnGameDataMessage(playerControl.NetId);
            RpcUtils.LateSpecificSendMessage(message, player.GetClientId());
        }

        protected virtual void OnFixedUpdate(bool lowload, int timerLowLoad)
        {
            //
            // Need to respawn player every 20s because of 30s timeout
            // 
            PlayerControlTimer += Time.fixedDeltaTime;
            if (PlayerControlTimer > 20f)
            {
                Logger.Info($" Recreate Custom Net Object {GetType().Name} (ID {Id})", "CNO.OnFixedUpdate");
                PlayerControl oldPlayerControl = playerControl;
                playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
                playerControl.PlayerId = 255;
                playerControl.isNew = false;
                playerControl.notRealPlayer = true;
                playerControl.NetTransform.SnapTo(new(-200, -200));

                AmongUsClient.Instance.Spawn(playerControl, -2, SpawnFlags.None);

                if (PlayerControl.AllPlayerControls.Contains(playerControl))
                    PlayerControl.AllPlayerControls.Remove(playerControl);
                _ = new LateTask(() =>
                {
                    playerControl.NetTransform.RpcSnapTo(Position);
                    playerControl.RawSetName(Sprite);

                    NetworkedPlayerInfo subPlayerInfo = UnityEngine.Object.Instantiate<NetworkedPlayerInfo>(PlayerControl.LocalPlayer.Data);
                    subPlayerInfo.NetId = PlayerControl.LocalPlayer.Data.NetId;
                    subPlayerInfo.ClientId = PlayerControl.LocalPlayer.Data.ClientId;
                    subPlayerInfo.PlayerId = PlayerControl.LocalPlayer.Data.PlayerId;
                    subPlayerInfo.name = "CNO_dummy";
                    subPlayerInfo.Outfits.Clear();
                    subPlayerInfo.FriendCode = PlayerControl.LocalPlayer.Data.FriendCode;
                    subPlayerInfo.Puid = GameStates.IsVanillaServer ? PlayerControl.LocalPlayer.Data.Puid : "";
                    subPlayerInfo.PlayerLevel = 250;
                    subPlayerInfo.Tasks.Clear();
                    subPlayerInfo.Role = PlayerControl.LocalPlayer.Data.Role;
                    subPlayerInfo.DespawnOnDestroy = false;

                    NetworkedPlayerInfo.PlayerOutfit playerOutfit = new();
                    playerOutfit.Set("<size=14><br></size>" + Sprite, 255, "", "", "", "", "");

                    subPlayerInfo.Outfits[PlayerOutfitType.Default] = playerOutfit;

                    var sender = CustomRpcSender.Create("SetFakeData");
                    MessageWriter writer = sender.stream;
                    sender.StartMessage();

                    writer.StartMessage(1);
                    {
                        writer.WritePacked(playerControl.NetId);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                    }
                    writer.EndMessage();

                    writer.StartMessage(1);
                    {
                        writer.WritePacked(subPlayerInfo.NetId);
                        subPlayerInfo.Serialize(writer, false);
                    }
                    writer.EndMessage();

                    sender.StartRpc(playerControl.NetId, (byte)RpcCalls.Shapeshift)
                        .WriteNetObject(PlayerControl.LocalPlayer)
                        .Write(false)
                        .EndRpc();

                    writer.StartMessage(1);
                    {
                        writer.WritePacked(playerControl.NetId);
                        writer.Write((byte)255);
                    }
                    writer.EndMessage();

                    writer.StartMessage(1);
                    {
                        writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                        PlayerControl.LocalPlayer.Data.Serialize(writer, false);
                    }

                    writer.EndMessage();
                    sender.EndMessage();
                    sender.SendMessage();

                    UnityEngine.Object.Destroy(subPlayerInfo.gameObject);
                }, 0.2f, "CNO_RespawnPlayerControl_SendData");

                _ = new LateTask(() => oldPlayerControl.Despawn(), 0.3f);

                foreach (var pc in Main.AllPlayerControls.Where(x => HiddenList.Contains(x.PlayerId)))
                {
                    Hide(pc);
                }
                _ = new LateTask(() =>
                { // Fix for host
                    if (!HiddenList.Contains(PlayerControl.LocalPlayer.PlayerId))
                        playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(true);
                }, 0.1f);
                _ = new LateTask(() =>
                { // Fix for Modded
                    foreach (var visiblePC in Main.AllPlayerControls.ExceptBy(HiddenList, x => x.PlayerId))
                    {
                        CustomRpcSender sender = CustomRpcSender.Create("FixModdedClientCNOText", sendOption: SendOption.Reliable);
                        sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixModdedClientCNO, visiblePC.GetClientId())
                            .WriteNetObject(playerControl)
                            .Write(true)
                            .EndRpc();
                        sender.SendMessage();
                    }
                }, 0.4f, "CNO_RespawnPlayerControl_FixModdedCNO");
                PlayerControlTimer = 0f;

                return;
            }

            // Host is the -2 owner of NT, dirty the NT and host will serialize it automatically.
            var NT = playerControl.NetTransform;

            if (NT == null) return;
            playerControl.Collider.enabled = false;
            if (Position != NT.body.position)
            {
                Transform transform = NT.transform;
                NT.body.position = Position;
                transform.position = Position;
                NT.body.velocity = Vector2.zero;
                NT.lastSequenceId++;
            }

            if (NT.HasMoved())
            {
                NT.sendQueue.Enqueue(NT.body.position);
                NT.SetDirtyBit(2U);
            }
        }

        protected virtual void OnAfterMeetingTasks()
        {
            playerControl.NetTransform.RpcSnapTo(Position);
        }

        public void CreateNetObject(string sprite, Vector2 position)
        {
            Logger.Info($" Create Custom Net Object {GetType().Name} (ID {Id}) at {position}", "CNO.CreateNetObject");
            playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
            playerControl.PlayerId = 255;
            playerControl.isNew = false;
            playerControl.notRealPlayer = true;
            playerControl.NetTransform.SnapTo(new(-200, -200));

            AmongUsClient.Instance.Spawn(playerControl, -2, SpawnFlags.None);

            if (PlayerControl.AllPlayerControls.Contains(playerControl)) PlayerControl.AllPlayerControls.Remove(playerControl);

            _ = new LateTask(() =>
            {
                playerControl.NetTransform.RpcSnapTo(position);
                playerControl.RawSetName(sprite);

                NetworkedPlayerInfo subPlayerInfo = UnityEngine.Object.Instantiate<NetworkedPlayerInfo>(PlayerControl.LocalPlayer.Data);
                subPlayerInfo.NetId = PlayerControl.LocalPlayer.Data.NetId;
                subPlayerInfo.ClientId = PlayerControl.LocalPlayer.Data.ClientId;
                subPlayerInfo.PlayerId = PlayerControl.LocalPlayer.Data.PlayerId;
                subPlayerInfo.name = "CNO_dummy";
                subPlayerInfo.Outfits.Clear();
                subPlayerInfo.FriendCode = PlayerControl.LocalPlayer.Data.FriendCode;
                subPlayerInfo.Puid = GameStates.IsVanillaServer ? PlayerControl.LocalPlayer.Data.Puid : "";
                subPlayerInfo.PlayerLevel = 250;
                subPlayerInfo.Tasks.Clear();
                subPlayerInfo.Role = PlayerControl.LocalPlayer.Data.Role;
                subPlayerInfo.DespawnOnDestroy = false;

                NetworkedPlayerInfo.PlayerOutfit playerOutfit = new();
                playerOutfit.Set("<size=14><br></size>" + Sprite, 255, "", "", "", "", "");

                subPlayerInfo.Outfits[PlayerOutfitType.Default] = playerOutfit;

                var sender = CustomRpcSender.Create("SetFakeData");
                MessageWriter writer = sender.stream;
                sender.StartMessage();

                writer.StartMessage(1);
                {
                    writer.WritePacked(playerControl.NetId);
                    writer.Write(PlayerControl.LocalPlayer.PlayerId);
                }
                writer.EndMessage();

                writer.StartMessage(1);
                {
                    writer.WritePacked(subPlayerInfo.NetId);
                    subPlayerInfo.Serialize(writer, false);
                }
                writer.EndMessage();

                sender.StartRpc(playerControl.NetId, (byte)RpcCalls.Shapeshift)
                    .WriteNetObject(PlayerControl.LocalPlayer)
                    .Write(false)
                    .EndRpc();

                writer.StartMessage(1);
                {
                    writer.WritePacked(playerControl.NetId);
                    writer.Write((byte)255);
                }
                writer.EndMessage();

                writer.StartMessage(1);
                {
                    writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                    PlayerControl.LocalPlayer.Data.Serialize(writer, false);
                }

                writer.EndMessage();
                sender.EndMessage();
                sender.SendMessage();

                UnityEngine.Object.Destroy(subPlayerInfo.gameObject);
            }, 0.2f);

            Position = position;
            playerControl.Collider.enabled = false;
            PlayerControlTimer = 0f;
            Sprite = sprite;
            ++MaxId;
            Id = MaxId;
            if (MaxId == int.MaxValue) MaxId = int.MinValue;
            AllObjects.Add(this);

            _ = new LateTask(() =>
            { // Fix for host
                playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(true);
            }, 0.1f);
            _ = new LateTask(() =>
            { // Fix for Modded
                CustomRpcSender sender = CustomRpcSender.Create("FixModdedClientCNOText", sendOption: SendOption.Reliable);
                sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixModdedClientCNO)
                    .WriteNetObject(playerControl)
                    .Write(true)
                    .EndRpc();
                sender.SendMessage();
            }, 0.4f, "CNO_CreatePlayerControl_FixModdedCNO");
        }
        public static void FixedUpdate(bool lowload, int timerLowLoad) => AllObjects.ToArray().Do(x => x.OnFixedUpdate(lowload, timerLowLoad));
        public static void AfterMeetingTasks() => AllObjects.ToArray().Do(x => x.OnAfterMeetingTasks());
        public static CustomNetObject Get(int id) => AllObjects.FirstOrDefault(x => x.Id == id);
        public static void DespawnOnQuit(byte Playerid) => AllObjects.Where(x => x.OwnerId == Playerid).ToArray().Do(x => x.Despawn());

        public static void Reset()
        {
            try
            {
                AllObjects.ToArray().Do(x => x.Despawn());
                AllObjects.Clear();
            }
            catch (Exception e)
            {
                Utils.ThrowException(e);
            }
        }
    }
    internal sealed class Explosion : CustomNetObject
    {
        private readonly float Duration;

        private readonly float Size;
        private int Frame;
        private float Timer;

        public Explosion(float size, float duration, Vector2 position)
        {
            Size = size;
            Duration = duration;
            Timer = -0.1f;
            Frame = 0;
            CreateNetObject($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>███<#ff0000>█<#0000>███<br><#ff0000>█<#0000>█<#ff0000>███<#0000>█<#ff0000>█<br>█<#ff8000>██<#ffff00>█<#ff8000>██<#ffff00>█<br>██<#ff8000>█<#ffff00>█<#ff8000>█<#ffff00>██<br><#ff8000>█<#ffff80>██<#ffff00>█<#ffff80>██<#ff8000>█<br><#0000>█<#ff8000>█<#ffff80>███<#ff8000>█<#0000>█<br>██<#ff8000>███<#0000>██", position);
        }

        protected override void OnFixedUpdate(bool lowload, int timerLowLoad)
        {
            base.OnFixedUpdate(lowload, timerLowLoad);

            Timer += Time.deltaTime;
            if (Timer >= Duration / 5f && Frame == 0)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>█<#ff0000>█<#0000>█<#ff0000>█<#0000>█<#ff0000>█<#0000>█<br><#ff0000>█<#ff8000>█<#ff0000>█<#ff8000>█<#ff0000>█<#ff8000>█<#ff0000>█<br><#ff8000>██<#ffff00>█<#ff8000>█<#ffff00>█<#ff8000>██<br><#ffff00>███████<br><#ff8000>█<#ffff00>█████<#ff8000>█<br>██<#ffff00>█<#ff8000>█<#ffff00>█<#ff8000>██<br><#ff0000>█<#0000>█<#ff8000>█<#ff0000>█<#ff8000>█<#0000>█<#ff0000>█");
                Frame = 1;
            }

            if (Timer >= Duration / 5f * 2f && Frame == 1)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>█<#c0c0c0>█<#ff0000>█<#000000>█<#ff0000>█<#c0c0c0>█<#0000>█<br><#c0c0c0>█<#808080>█<#ff0000>█<#ff8000>█<#ff0000>█<#c0c0c0>██<br><#ff0000>██<#ff8000>█<#ffff00>█<#ff8000>█<#ff0000>██<br><#c0c0c0>█<#ff8000>█<#ffff00>█<#ffff80>█<#ffff00>█<#ff8000>█<#808080>█<br><#ff0000>██<#ff8000>█<#ffff00>█<#ff8000>█<#ff0000>██<br><#c0c0c0>█<#808080>█<#ff0000>█<#ff8000>█<#ff0000>█<#000000>█<#c0c0c0>█<br><#0000>█<#c0c0c0>█<#ff0000>█<#c0c0c0>█<#ff0000>█<#c0c0c0>█<#0000>█");
                Frame = 2;
            }

            if (Timer >= Duration / 5f * 3f && Frame == 2)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#ff0000>█<#ff8000>█<#0000>█<#808080>█<#0000>█<#ff8000>█<#ff0000>█<br><#ff8000>█<#0000>█<#ffff00>█<#c0c0c0>█<#ffff00>█<#0000>█<#ff8000>█<br><#0000>█<#ffff00>█<#c0c0c0>███<#ffff00>█<#0000>█<br><#808080>█<#c0c0c0>█████<#808080>█<br><#0000>█<#ffff00>█<#c0c0c0>███<#ffff00>█<#0000>█<br><#ff8000>█<#0000>█<#ffff00>█<#c0c0c0>█<#ffff00>█<#0000>█<#ff8000>█<br><#ff0000>█<#ff8000>█<#0000>█<#808080>█<#0000>█<#ff8000>█<#ff0000>█");
                Frame = 3;
            }

            if (Timer >= Duration / 5f * 4f && Frame == 3)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>█<#808080>█<#0000>██<#c0c0c0>█<#0000>█<#808080>█<br><#ffff00>█<#0000>██<#c0c0c0>█<#0000>█<#808080>█<#0000>█<br>█<#808080>█<#c0c0c0>████<#0000>█<br>█<#c0c0c0>██████<br>█<#0000>█<#c0c0c0>███<#808080>█<#0000>█<br>█<#c0c0c0>█<#0000>█<#c0c0c0>█<#0000>█<#c0c0c0>██<br><#808080>█<#0000>█<#c0c0c0>█<#0000>█<#808080>█<#0000>█<#ffff00>█");
                Frame = 4;
            }

            if (Timer >= Duration && Frame == 4)
            {
                Despawn();
            }
        }
    }
    internal sealed class BlackHole : CustomNetObject
    {
        internal BlackHole(Vector2 position, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return; // Spawning gets ignored for abyssbringer RPC, because it already does an rpc as Host
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#000000>█<#19131c>█<#000000>█<#000000>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#412847>█<#000000>█<#19131c>█<#000000>█<#412847>█<#260f26>█<alpha=#00>█<br><#000000>█<#412847>█<#412847>█<#000000>█<#260f26>█<#1c0d1c>█<#19131c>█<#000000>█<br><#19131c>█<#000000>█<#412847>█<#1c0d1c>█<#1c0d1c>█<#000000>█<#19131c>█<#000000>█<br><#000000>█<#000000>█<#260f26>█<#1c0d1c>█<#1c0d1c>█<#000000>█<#000000>█<#260f26>█<br><#000000>█<#260f26>█<#1c0d1c>█<#1c0d1c>█<#19131c>█<#412847>█<#412847>█<#19131c>█<br><alpha=#00>█<#260f26>█<#412847>█<#412847>█<#19131c>█<#260f26>█<#19131c>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#412847>█<#260f26>█<#260f26>█<#000000>█<alpha=#00>█<alpha=#00>█<br></line-height></size>", position);
            this.OwnerId = OwnerId;
        }
    }
    internal sealed class Firework : CustomNetObject
    {
        internal Firework(Vector2 position, List<byte> visibleList, byte OwnerId)
        {
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<alpha=#00>█<#f2ce1c>█<#f2eb0d>█<alpha=#00>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#f2eb0d>█<#f2eb0d>█<#f2ce1c>█<#f2eb0d>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#f2eb0d>█<#f2eb0d>█<#f2eb0d>█<#f2eb0d>█<#f2ce1c>█<#f2eb0d>█<alpha=#00>█<br><alpha=#00>█<#e60000>█<#e60000>█<#f2f2f2>█<#e60000>█<#e60000>█<#f2f2f2>█<alpha=#00>█<br><alpha=#00>█<#f2f2f2>█<#f20d0d>█<#f20d0d>█<#f2f2f2>█<#f20d0d>█<#e60000>█<alpha=#00>█<br><#f2740d>█<#f2740d>█<#f2f2f2>█<#f20d0d>█<#f20d0d>█<#f2f2f2>█<#e60000>█<#f2740d>█<br><#f2740d>█<#f2740d>█<#f2740d>█<#f2f2f2>█<#f20d0d>█<#f20d0d>█<#f2740d>█<#f2740d>█<br><#cb5f06>█<#cb5f06>█<#cb5f06>█<#f20d0d>█<#f2f2f2>█<#cb5f06>█<#cb5f06>█<#cb5f06>█<br></color></line-height></font></size>", position);
            Main.AllAlivePlayerControls.ExceptBy(visibleList, x => x.PlayerId).Do(Hide);
            this.OwnerId = OwnerId;
        }
    }
    internal sealed class RiftPortal : CustomNetObject
    {
        internal RiftPortal(Vector2 position, List<byte> visibleList, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return; // Spawning gets ignored for rift maker RPC, because it already does an rpc as Host
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#e81111>█<#e81111>█<#e81111>█<#e81111>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#e81111>█<#ac2020>█<#ac2020>█<#ac2020>█<#ac2020>█<#e81111>█<alpha=#00>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#db5c5c>█<#db5c5c>█<#db5c5c>█<#ac2020>█<#e81111>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#ac2020>█<#ac2020>█<#db5c5c>█<#ac2020>█<#e81111>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#ac2020>█<#db5c5c>█<#db5c5c>█<#ac2020>█<#e81111>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#ac2020>█<#ac2020>█<#ac2020>█<#ac2020>█<#e81111>█<br><alpha=#00>█<#e81111>█<#ac2020>█<#db5c5c>█<#db5c5c>█<#db5c5c>█<#e81111>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#e81111>█<#ac2020>█<#ac2020>█<#ac2020>█<alpha=#00>█<alpha=#00>█<br></color></line-height></font></size>", position);
            Main.AllAlivePlayerControls.ExceptBy(visibleList, x => x.PlayerId).Do(Hide);
            this.OwnerId = OwnerId;
        }
    }

}
