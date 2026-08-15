using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Commands;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class CondemnationPower : ModPowerTemplate
{
    public const int JudgmentThreshold = 7;
    public const int DamagePerLayer = 7;

    private bool _isJudging;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MaidenSuccubus/images/ui/corruption_meter.svg",
        BigIconPath: "res://MaidenSuccubus/images/ui/corruption_meter.svg");

    internal void FlashForJudgment() => Flash();

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || _isJudging || Amount < JudgmentThreshold)
        {
            return;
        }

        _isJudging = true;
        try
        {
            await CondemnationCmd.Judge(
                choiceContext,
                Owner,
                force: true);
        }
        finally
        {
            _isJudging = false;
        }
    }
}
