using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;

namespace TOHE.Modules;

public class NormalGameOptionsSender : GameOptionsSender
{
    private LogicOptions _logicOptions;
    private static IGameOptions BasedGameOptions => GameOptionsManager.Instance.CurrentGameOptions;

    protected override bool IsDirty
    {
        get
        {
            try
            {
                if (GameManager.Instance != null && GameManager.Instance.LogicComponents != null && (_logicOptions == null || !GameManager.Instance.LogicComponents.Contains(_logicOptions)))
                {
                    foreach (GameLogicComponent glc in GameManager.Instance.LogicComponents)
                    {
                        if (glc.TryCast(out LogicOptions lo))
                            _logicOptions = lo;
                    }
                }

                return _logicOptions is { IsDirty: true };
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "NormalGameOptionsSender.IsDirty.Get");
                return _logicOptions is { IsDirty: true };
            }
        }
        set => _logicOptions?.ClearDirtyFlag();
    }

    public override IGameOptions BuildGameOptions()
    {
        return BasedGameOptions;
    }
    
    protected override void SendOptionsArray(Il2CppStructArray<byte> optionArray, byte logicOptionsIndex)
    {
        DataFlagRateLimiter.Enqueue(() =>
        {
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);

            writer.StartMessage(5);
            {
                writer.Write(AmongUsClient.Instance.GameId);

                writer.StartMessage(1);
                {
                    writer.WritePacked(GameManager.Instance.NetId);
                    writer.StartMessage(logicOptionsIndex);
                    {
                        writer.WriteBytesAndSize(optionArray);
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();
            }

            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        });
    }
}