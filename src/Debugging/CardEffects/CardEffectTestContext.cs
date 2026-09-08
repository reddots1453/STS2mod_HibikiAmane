#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>
/// Destructive, deterministic fixture around the currently active disposable combat.
/// Every assertion observes game state after real game commands have resolved.
/// </summary>
internal sealed class CardEffectTestContext
{
    private const int FixtureHp = 20_000;
    private readonly List<CardModel> _createdCards = [];
    private CardEffectScenarioResult? _result;

    public CombatState Combat { get; }
    public Player Player { get; }
    public Creature Self => Player.Creature;
    public Creature PrimaryEnemy => Combat.HittableEnemies.First();
    public IReadOnlyList<Creature> Enemies => Combat.HittableEnemies.ToArray();

    public CardEffectTestContext(CombatState combat, Player player)
    {
        Combat = combat;
        Player = player;
    }

    public void BeginScenario(CardEffectScenarioResult result) => _result = result;

    public async Task PrepareSuite()
    {
        if (Combat.HittableEnemies.Count == 0)
            throw new InvalidOperationException("A living enemy is required.");

        foreach (RelicModel relic in Player.Relics.ToArray())
            await RelicCmd.Remove(relic);

        await CreatureCmd.SetMaxAndCurrentHp(Self, FixtureHp);
        foreach (Creature enemy in Combat.HittableEnemies)
            await CreatureCmd.SetMaxAndCurrentHp(enemy, FixtureHp);
        await Reset();
    }

    public async Task Reset()
    {
        // This deliberately destroys the current combat state.  The console command
        // requires an explicit confirmation token and documents disposable-run use.
        foreach (CardPile pile in Player.Piles.Where(pile => pile.IsCombatPile))
        {
            CardModel[] cards = pile.Cards.ToArray();
            CardModel[] cardsWithNodes = cards
                .Where(card => NCard.FindOnTable(card) != null)
                .ToArray();
            if (cardsWithNodes.Length > 0)
            {
                await CardPileCmd.RemoveFromCombat(
                    cardsWithNodes,
                    skipVisuals: false);
            }
            CardModel[] cardsWithoutNodes = cards
                .Except(cardsWithNodes)
                .Where(card => card.Pile?.IsCombatPile == true)
                .ToArray();
            if (cardsWithoutNodes.Length > 0)
            {
                await CardPileCmd.RemoveFromCombat(
                    cardsWithoutNodes,
                    skipVisuals: true);
            }
        }
        _createdCards.Clear();

        foreach (Creature creature in Combat.Creatures)
        {
            foreach (PowerModel power in creature.Powers.ToArray())
                await PowerCmd.Remove(power);
            if (creature.Block > 0)
                await CreatureCmd.LoseBlock(
                    new BlockingPlayerChoiceContext(), creature, creature.Block, null);
            await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);
        }

        Creature[] playerTargets = Combat.Players.Select(player => player.Creature).ToArray();
        foreach (Creature enemy in Combat.Creatures.Where(creature => creature.Monster != null))
        {
            enemy.Monster!.ResetStateMachine();
            enemy.Monster.SetUpForCombat();
            enemy.Monster!.RollMove(playerTargets);
        }

        await PlayerCmd.SetEnergy(20, Player);
        await Desire.Set(Player, 0);
        if (Player.RunState is MegaCrit.Sts2.Core.Runs.RunState runState)
            CorruptionCmd.Set(runState, 0);
    }

    public CardModel Create(Type cardType, bool upgraded = false)
    {
        CardModel canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(cardType));
        CardModel card = Combat.CreateCard(canonical, Player);
        if (upgraded)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
        _createdCards.Add(card);
        return card;
    }

    public T Create<T>(bool upgraded = false) where T : CardModel =>
        (T)Create(typeof(T), upgraded);

    public async Task<T> Add<T>(
        PileType pile,
        bool upgraded = false,
        bool skipVisuals = true)
        where T : CardModel
    {
        T card = Create<T>(upgraded);
        await CardPileCmd.Add(card, pile, skipVisuals: skipVisuals);
        return card;
    }

    public async Task<CardModel> Add(
        Type cardType,
        PileType pile,
        bool upgraded = false,
        bool skipVisuals = true)
    {
        CardModel card = Create(cardType, upgraded);
        await CardPileCmd.Add(card, pile, skipVisuals: skipVisuals);
        return card;
    }

    public async Task<IReadOnlyList<CardModel>> AddFillerCards(
        PileType pile,
        int count,
        int cost = 1,
        CardType type = CardType.Attack)
    {
        List<CardModel> cards = [];
        for (int i = 0; i < count; i++)
        {
            CardModel card = type == CardType.Skill
                ? Create<DefendIronclad>()
                : Create<StrikeIronclad>();
            if (cost != 1)
                card.EnergyCost.SetThisCombat(cost);
            await CardPileCmd.Add(card, pile, skipVisuals: true);
            cards.Add(card);
        }
        return cards;
    }

    public async Task Play(
        CardModel card,
        Creature? target = null,
        IEnumerable<CardModel>? selectedCards = null,
        IEnumerable<int>? selectedIndices = null)
    {
        if (card.Pile == null)
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);

        TestCardSelector selector = new();
        if (selectedCards != null)
            selector.PrepareToSelect(selectedCards);
        else if (selectedIndices != null)
            selector.PrepareToSelect(selectedIndices);

        using IDisposable scope = CardSelectCmd.UseSelector(selector);
        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            target,
            skipCardPileVisuals: true);
    }

    public async Task ApplyPower<T>(Creature target, int amount)
        where T : PowerModel =>
        await PowerCmd.Apply<T>(
            new BlockingPlayerChoiceContext(), target, amount, Self, null);

    public async Task InvokeTurnEndInHand(CardModel card)
    {
        System.Reflection.MethodInfo method = typeof(CardModel).GetMethod(
            "OnTurnEndInHand",
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(CardModel).FullName, "OnTurnEndInHand");
        object? result = method.Invoke(card, [new BlockingPlayerChoiceContext()]);
        if (result is Task task)
            await task;
    }

    public int PowerAmount<T>(Creature creature) where T : PowerModel =>
        creature.Powers.OfType<T>().Sum(power => power.Amount);

    public int PowerAmount(Creature creature, string powerTypeName) =>
        creature.Powers
            .Where(power => power.GetType().Name == powerTypeName)
            .Sum(power => power.Amount);

    public int CountCards<T>(PileType pile) where T : CardModel =>
        pile.GetPile(Player).Cards.Count(card => card is T);

    public int CountCards(Type cardType, PileType pile) =>
        pile.GetPile(Player).Cards.Count(card => card.GetType() == cardType);

    public void AssertEqual<T>(string name, T expected, T actual, bool effect = true)
    {
        bool passed = EqualityComparer<T>.Default.Equals(expected, actual);
        Result.Assertions.Add(new CardEffectAssertionResult
        {
            Name = name,
            Passed = passed,
            Expected = expected?.ToString() ?? "null",
            Actual = actual?.ToString() ?? "null",
        });
        if (effect)
            Result.EffectAssertionCount++;
    }

    public void AssertTrue(string name, bool actual, bool effect = true) =>
        AssertEqual(name, true, actual, effect);

    public void AssertDamage(string name, Creature target, int hpBefore, int expected) =>
        AssertEqual(name, expected, hpBefore - target.CurrentHp);

    public void AssertBlock(string name, int blockBefore, int expected) =>
        AssertEqual(name, expected, Self.Block - blockBefore);

    public void AssertPower<T>(string name, Creature target, int expected)
        where T : PowerModel =>
        AssertEqual(name, expected, PowerAmount<T>(target));

    public void AssertPower(
        string name,
        Creature target,
        string powerTypeName,
        int expected) =>
        AssertEqual(name, expected, PowerAmount(target, powerTypeName));

    public void AssertPileDelta<T>(
        string name,
        PileType pile,
        int before,
        int expectedDelta)
        where T : CardModel =>
        AssertEqual(name, expectedDelta, CountCards<T>(pile) - before);

    public void AssertCardTypePileDelta(
        string name,
        Type cardType,
        PileType pile,
        int before,
        int expectedDelta) =>
        AssertEqual(name, expectedDelta, CountCards(cardType, pile) - before);

    public void AssertMetadata(
        CardModel card,
        int expectedCost,
        CardType expectedType,
        TargetType expectedTarget)
    {
        AssertEqual("energy cost", expectedCost, card.EnergyCost.Canonical, effect: false);
        AssertEqual("card type", expectedType, card.Type, effect: false);
        AssertEqual("target type", expectedTarget, card.TargetType, effect: false);
    }

    private CardEffectScenarioResult Result => _result
        ?? throw new InvalidOperationException("No active card effect test scenario.");
}
#endif
