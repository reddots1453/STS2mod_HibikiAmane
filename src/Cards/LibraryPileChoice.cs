using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LibraryPileChoice : MSGeneratedCard, ICardTitleContributor
{
    public PileType SelectedPile { get; private set; } = PileType.Draw;
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("PileName", "抽牌堆"), new DynamicVar("Count", 10)];
    public LibraryPileChoice() : base(-1, CardType.Skill, CardRarity.Token, TargetType.None) { }

    private string PileName => ((StringVar)DynamicVars["PileName"]).StringValue;
    public override string Title => PileName;

    public IEnumerable<CardTitleFragment> GetTitleFragments(CardTitleContext context)
    {
        var title = new LocString("cards", Id.Entry + ".pileTitle");
        title.Add("PileName", PileName);
        return [new(title, CardTitleFragmentPlacement.ReplaceBase)];
    }

    public void Configure(PileType pile, int count = 10)
    {
        DynamicVars["Count"].BaseValue = count;
        SelectedPile = pile;
        ((StringVar)DynamicVars["PileName"]).StringValue = pile switch
        {
            PileType.Draw => "抽牌堆",
            PileType.Discard => "弃牌堆",
            PileType.Exhaust => "消耗牌堆",
            _ => throw new ArgumentOutOfRangeException(nameof(pile)),
        };
    }
}
