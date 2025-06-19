using Hazel;
using Il2CppInterop.Generator.Extensions;
using TOHE.Roles.Crewmate;

namespace TOHE.Modules.Rpc
{
    class RpcGuessKill : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.GuessKill;
        public RpcGuessKill(uint netId, byte playerId) : base(netId)
        {
            this.playerId = playerId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
        }

        private readonly byte playerId;
    }

    class RpcJudge : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.Judge;
        public RpcJudge(uint netId, byte playerId) : base(netId)
        {
            this.playerId = playerId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
        }

        private readonly byte playerId;
    }

    class RpcCouncillorJudge : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.CouncillorJudge;
        public RpcCouncillorJudge(uint netId, byte playerId) : base(netId)
        {
            this.playerId = playerId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
        }

        private readonly byte playerId;
    }

    class RpcNemesisRevenge : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.NemesisRevenge;
        public RpcNemesisRevenge(uint netId, byte playerId) : base(netId)
        {
            this.playerId = playerId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
        }

        private readonly byte playerId;
    }

    class RpcRetributionistRevenge : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.RetributionistRevenge;
        public RpcRetributionistRevenge(uint netId, byte playerId) : base(netId)
        {
            this.playerId = playerId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
        }

        private readonly byte playerId;
    }

    class RpcSetBountyTarget : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetBountyTarget;
        public RpcSetBountyTarget(uint netId, byte bountyId, byte targetId) : base(netId)
        {
            this.bountyId = bountyId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(bountyId);
            msg.Write(targetId);
        }

        private readonly byte bountyId;
        private readonly byte targetId;
    }

    class RpcSyncPuppet : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncPuppet;
        public RpcSyncPuppet(uint netId, byte typeId, byte puppetId, byte targetId) : base(netId)
        {
            this.typeId = typeId;
            this.puppetId = puppetId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(typeId);
            msg.Write(puppetId);
            msg.Write(targetId);
        }

        private readonly byte typeId;
        private readonly byte puppetId;
        private readonly byte targetId;
    }

    class RpcSetKillOrSpell : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetKillOrSpell;
        public RpcSetKillOrSpell(uint netId, byte playerId, bool spellMode) : base(netId)
        {
            this.playerId = playerId;
            this.spellMode = spellMode;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(spellMode);
        }

        private readonly byte playerId;
        private readonly bool spellMode;
    }

    class RpcSetDousedPlayer : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetDousedPlayer;
        public RpcSetDousedPlayer(uint netId, byte playerId, byte targetId, bool isDoused) : base(netId)
        {
            this.playerId = playerId;
            this.targetId = targetId;
            this.isDoused = isDoused;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(targetId);
            msg.Write(isDoused);
        }

        private readonly byte playerId;
        private readonly byte targetId;
        private readonly bool isDoused;
    }

    class RpcDoSpell : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.DoSpell;
        public RpcDoSpell(uint netId, byte witchId, byte targetId) : base(netId)
        {
            this.witchId = witchId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(witchId);
            msg.Write(targetId);
        }

        private readonly byte witchId;
        private readonly byte targetId;
    }

    class RpcDoHex : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.DoHex;
        public RpcDoHex(uint netId, byte hexId, byte targetId) : base(netId)
        {
            this.hexId = hexId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(hexId);
            msg.Write(targetId);
        }

        private readonly byte hexId;
        private readonly byte targetId;
    }

    class RpcSniperSync : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SniperSync;
        public RpcSniperSync(uint netId, byte playerId, List<byte> snList) : base(netId)
        {
            this.playerId = playerId;
            this.snList = snList;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(snList.Count);
            foreach (var sn in snList)
            {
                msg.Write(sn);
            }
        }

        private readonly byte playerId;
        private readonly List<byte> snList;
    }

    class RpcSetLoversPlayers : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetLoversPlayers;
        public RpcSetLoversPlayers(uint netId, int count, HashSet<PlayerControl> loversList) : base(netId)
        {
            this.count = count;
            this.loversList = loversList;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(count);
            foreach (var lp in loversList)
            {
                msg.Write(lp.PlayerId);
            }
        }

        private readonly int count;
        private readonly HashSet<PlayerControl> loversList;
    }

    class RpcSendFireworkerState : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SendFireworkerState;
        public RpcSendFireworkerState(uint netId, byte playerId, int nowFireworkerCount, int state) : base(netId)
        {
            this.playerId = playerId;
            this.nowFireworkerCount = nowFireworkerCount;
            this.state = state;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(nowFireworkerCount);
            msg.Write(state);
        }

        private readonly byte playerId;
        private readonly int nowFireworkerCount;
        private readonly int state;
    }

    class RpcSetCurrentDousingTarget : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetCurrentDousingTarget;
        public RpcSetCurrentDousingTarget(uint netId, byte arsonistId, byte targetId) : base(netId)
        {
            this.arsonistId = arsonistId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(arsonistId);
            msg.Write(targetId);
        }

        private readonly byte arsonistId;
        private readonly byte targetId;
    }

    class RpcSetEvilTrackerTarget : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetEvilTrackerTarget;
        public RpcSetEvilTrackerTarget(uint netId, byte trackerId, byte targetId) : base(netId)
        {
            this.trackerId = trackerId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(trackerId);
            msg.Write(targetId);
        }

        private readonly byte trackerId;
        private readonly byte targetId;
    }

    class RpcSetDrawPlayer : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetDrawPlayer;
        public RpcSetDrawPlayer(uint netId, byte playerId, byte targetId, bool isDrawed) : base(netId)
        {
            this.playerId = playerId;
            this.targetId = targetId;
            this.isDrawed = isDrawed;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.Write(targetId);
            msg.Write(isDrawed);
        }

        private readonly byte playerId;
        private readonly byte targetId;
        private readonly bool isDrawed;
    }
    class RpcSetCrewpostorTasksDone : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetCrewpostorTasksDone;
        public RpcSetCrewpostorTasksDone(uint netId, byte playerId, int tasksDone) : base(netId)
        {
            this.playerId = playerId;
            this.tasksDone = tasksDone;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.WritePacked(tasksDone);
        }

        private readonly byte playerId;
        private readonly int tasksDone;
    }
    class RpcSetCurrentDrawTarget : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetCurrentDrawTarget;
        public RpcSetCurrentDrawTarget(uint netId, byte revoId, byte targetId) : base(netId)
        {
            this.revoId = revoId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(revoId);
            msg.Write(targetId);
        }

        private readonly byte revoId;
        private readonly byte targetId;
    }
    class RpcSyncJailerData : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncJailerData;
        public RpcSyncJailerData(uint netId, byte playerId, int jailerTarget, bool hasExe, bool didVote) : base(netId)
        {
            this.playerId = playerId;
            this.jailerTarget = jailerTarget;
            this.hasExe = hasExe;
            this.didVote = didVote;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.WritePacked(jailerTarget);
            msg.Write(hasExe);
            msg.Write(didVote);
        }

        private readonly byte playerId;
        private readonly int jailerTarget;
        private readonly bool hasExe;
        private readonly bool didVote;
    }

    class RpcSetInspectorLimit : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetInspectorLimit;
        public RpcSetInspectorLimit(uint netId, byte playerId, int limit) : base(netId)
        {
            this.playerId = playerId;
            this.limit = limit;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.WritePacked(limit);
        }

        private readonly byte playerId;
        private readonly int limit;
    }
    class RpcKeeper : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.KeeperRPC;
        public RpcKeeper(uint netId, int type, byte playerId, byte targetId) : base(netId)
        {
            this.type = type;
            this.playerId = playerId;
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(type);
            if (type == 0)
            {
                msg.Write(playerId);
                msg.Write(targetId);
            }
        }

        private readonly int type;
        private readonly byte playerId;
        private readonly byte targetId;
    }
    class RpcGuess : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.Guess;
        public RpcGuess(uint netId, byte playerId, CustomRoles role) : base(netId)
        {
            this.playerId = playerId;
            this.role = role;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(playerId);
            msg.WritePacked((int)role);
        }

        private readonly byte playerId;
        private readonly CustomRoles role;
    }
}
