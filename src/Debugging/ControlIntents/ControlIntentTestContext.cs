#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.ControlIntents;

/// <summary>
/// Deterministic fixture over the active disposable combat. Assertions observe
/// model state after real game commands and card play wrappers have completed.
/// </summary>
internal sealed class ControlIntentTestContext
{
    private readonly CardEffectTestContext _fixture;
    private readonly List<Creature> _addedEnemies = [];
    private ControlIntentScenarioResult? _result;

    public MegaCrit.Sts2.Core.Combat.CombatState Combat => _fixture.Combat;
    public Player Player => _fixture.Player;
    public Creature Self => _fixture.Self;
    public Creature PrimaryEnemy => _fixture.PrimaryEnemy;

    public ControlIntentTestContext(
        MegaCrit.Sts2.Core.Combat.CombatState combat,
        Player player)
    {
        _fixture = new CardEffectTestContext(combat, player);
    }

    public Task PrepareSuite() => _fixture.PrepareSuite();

    public void BeginScenario(ControlIntentScenarioResult result) => _result = result;

    public async Task Reset()
    {
        await RemoveAddedEnemies();
        await _fixture.Reset();
    }

    public async Task Finish()
    {
        await RemoveAddedEnemies();
        await _fixture.Reset();
    }

    public void Checkpoint(string value)
    {
        Result.LastCheckpoint = value;
        MaidenSuccubusMod.Logger.Info(
            $"[ControlIntentTest] CHECKPOINT {Result.Name}: {value}");
    }

    public Task<T> AddCard<T>(
        PileType pile,
        bool upgraded = false)
        where T : CardModel =>
        _fixture.Add<T>(pile, upgraded, skipVisuals: true);

    public async Task<Creature> AddByrdonis()
    {
        Creature creature = await CreatureCmd.Add<Byrdonis>(
            Self.CombatState
            ?? throw new InvalidOperationException("No active combat state."));
        _addedEnemies.Add(creature);
        await CreatureCmd.SetMaxAndCurrentHp(creature, 20_000);
        return creature;
    }

    public async Task<Creature> AddFossilStalker()
    {
        Creature creature = await CreatureCmd.Add<FossilStalker>(
            Self.CombatState
            ?? throw new InvalidOperationException("No active combat state."));
        _addedEnemies.Add(creature);
        await CreatureCmd.SetMaxAndCurrentHp(creature, 20_000);
        return creature;
    }

    public async Task<Creature> AddCorpseSlug()
    {
        Creature creature = await CreatureCmd.Add<CorpseSlug>(
            Self.CombatState
            ?? throw new InvalidOperationException("No active combat state."));
        _addedEnemies.Add(creature);
        await CreatureCmd.SetMaxAndCurrentHp(creature, 20_000);
        return creature;
    }

    public async Task<Creature> AddTerrorEel()
    {
        Creature creature = await CreatureCmd.Add<TerrorEel>(
            Self.CombatState
            ?? throw new InvalidOperationException("No active combat state."));
        _addedEnemies.Add(creature);
        await CreatureCmd.SetMaxAndCurrentHp(creature, 20_000);
        return creature;
    }

    public async Task<ControlPower> ApplyControl(
        Creature source,
        ControlType type,
        int amount)
    {
        ControlPower power = (ControlPower)ModelDb
            .Power<ControlPower>()
            .ToMutable();
        power.ControlType = type;
        await PowerCmd.Apply(
            new BlockingPlayerChoiceContext(),
            power,
            Self,
            amount,
            source,
            null,
            silent: true);
        return Self.Powers
            .OfType<ControlPower>()
            .Single(candidate => ReferenceEquals(candidate.Applier, source));
    }

    public async Task<int> PlayPayingResources(CardModel card)
    {
        AssertIdle();
        if (card.Pile == null)
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);

        int energyBefore = Player.PlayerCombatState!.Energy;
        (int energySpent, int starsSpent) = await card.SpendResources();
        ResourceInfo resources = new()
        {
            EnergySpent = energySpent,
            EnergyValue = energySpent,
            StarsSpent = starsSpent,
            StarValue = starsSpent,
        };
        await card.OnPlayWrapper(
            new BlockingPlayerChoiceContext(),
            null,
            isAutoPlay: true,
            resources,
            skipCardPileVisuals: true);
        AssertEqual(
            "resource payment matches SpendResources",
            energySpent,
            energyBefore - Player.PlayerCombatState!.Energy);
        return energySpent;
    }

    public void AssertEqual<T>(string name, T expected, T actual)
    {
        Result.Assertions.Add(new ControlIntentAssertionResult
        {
            Name = name,
            Passed = EqualityComparer<T>.Default.Equals(expected, actual),
            Expected = expected?.ToString() ?? "null",
            Actual = actual?.ToString() ?? "null",
        });
    }

    public void AssertTrue(string name, bool actual) =>
        AssertEqual(name, true, actual);

    public void AssertReference(string name, object expected, object? actual) =>
        AssertTrue(name, ReferenceEquals(expected, actual));

    public void AssertNoControl(string name) =>
        AssertEqual(name, 0, ControlQuery.GetInstances(Player).Count);

    private void AssertIdle()
    {
        if (Self.CombatState == null)
            throw new InvalidOperationException("The disposable combat ended during the suite.");
    }

    private async Task RemoveAddedEnemies()
    {
        for (int index = _addedEnemies.Count - 1; index >= 0; index--)
        {
            Creature creature = _addedEnemies[index];
            if (creature.CombatState != null)
                await CreatureCmd.Escape(creature);
        }
        _addedEnemies.Clear();
    }

    private ControlIntentScenarioResult Result => _result
        ?? throw new InvalidOperationException("No active control-intent scenario.");
}
#endif
