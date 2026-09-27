using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Enchantments;

/// <summary>
/// Technical single-slot adapter for LightWings. Children remain real native
/// models with independent first-play state, not copies of their algorithms.
/// </summary>
[RegisterEnchantment]
public sealed class LayeredEnchantment : ModEnchantmentTemplate
{
    private List<EnchantmentModel> _layers = [];

    // SavedProperties only supports scalar strings, not enchantment arrays.
    // Native child SerializableEnchantment/SavedProperties are embedded without
    // serializing live Card/Owner objects. The outer native packet carries this
    // same string, so save and multiplayer copies use an identical payload.
    [SavedProperty]
    public string LayerData
    {
        get => LayeredEnchantmentSerialization.Serialize(_layers.Select(layer => layer.ToSerializable()));
        set
        {
            AssertMutable();
            var saved = LayeredEnchantmentSerialization.Deserialize(value);
            if (saved.Any(item => item.Id == ModelDb.GetId<LayeredEnchantment>()))
                throw new InvalidOperationException("Nested layered enchantments are not supported.");
            _layers = saved.Select(EnchantmentModel.FromSerializable).ToList();
        }
    }

    internal IReadOnlyList<EnchantmentModel> Layers
    {
        get
        {
            if (IsMutable && HasCard)
                foreach (var layer in _layers)
                    if (!layer.HasCard) layer.ApplyInternal(Card, layer.Amount);
            return _layers;
        }
    }

    public override EnchantmentAssetProfile AssetProfile => new(ModelDb.Enchantment<Glam>().IconPath);
    public override bool CanEnchant(CardModel card) => false; // Container is never a selectable effect.
    public override bool ShowAmount => true;
    public override int DisplayAmount => _layers.Count;
    public override bool ShouldGlowGold => Layers.Any(layer => layer.ShouldGlowGold);
    public override bool ShouldGlowRed => Layers.Any(layer => layer.ShouldGlowRed);
    public override bool ShouldStartAtBottomOfDrawPile => Layers.Any(layer => layer.ShouldStartAtBottomOfDrawPile);
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Layers.SelectMany(layer => layer.HoverTips);

    internal void Seed(IEnumerable<EnchantmentModel> layers)
    {
        AssertMutable();
        _layers = layers.ToList();
    }

    internal EnchantmentModel Add(EnchantmentModel incoming, decimal amount)
    {
        AssertMutable();
        _ = Layers; // Bind any legacy single-slot layer to this card first.
        EnchantmentModel? existing = incoming.IsStackable
            ? Layers.FirstOrDefault(layer => layer.GetType() == incoming.GetType()) : null;
        if (existing != null)
        {
            existing.Amount += (int)amount;
            return existing;
        }
        incoming.ApplyInternal(Card, amount);
        _layers.Add(incoming);
        incoming.ModifyCard(); // Never reapply existing cost/keyword mutations.
        return incoming;
    }

    internal void ClearChildren()
    {
        foreach (var layer in _layers)
            if (layer.HasCard) layer.ClearInternal();
    }

    internal string Summary => string.Join("、", Layers.GroupBy(layer => layer.Id).Select(group =>
    {
        string title = group.First().Title.GetFormattedText();
        if (group.Count() > 1) title += " ×" + group.Count();
        if (group.First().ShowAmount) title += "（" + string.Join("+", group.Select(layer => layer.DisplayAmount)) + "）";
        return title;
    }));

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _layers = _layers.Select(layer => (EnchantmentModel)layer.ClonePreservingMutability()).ToList();
    }

    protected override void OnEnchant()
    {
        foreach (var layer in Layers) layer.ModifyCard();
    }

    public override void RecalculateValues()
    {
        foreach (var layer in Layers) layer.RecalculateValues();
    }

    public override decimal EnchantDamageAdditive(decimal damage, ValueProp props) =>
        Layers.Sum(layer => layer.EnchantDamageAdditive(damage, props));
    public override decimal EnchantDamageMultiplicative(decimal damage, ValueProp props) =>
        Layers.Aggregate(1m, (factor, layer) => factor * layer.EnchantDamageMultiplicative(damage, props));
    public override decimal EnchantBlockAdditive(decimal block) =>
        Layers.Sum(layer => layer.EnchantBlockAdditive(block));
    public override decimal EnchantBlockMultiplicative(decimal block) =>
        Layers.Aggregate(1m, (factor, layer) => factor * layer.EnchantBlockMultiplicative(block));
    public override int EnchantPlayCount(int count) =>
        Layers.Aggregate(count, (current, layer) => layer.EnchantPlayCount(current));

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? play)
    {
        foreach (var layer in Layers.ToArray())
        {
            await layer.OnPlay(context, play);
            layer.InvokeExecutionFinished();
        }
    }
}
