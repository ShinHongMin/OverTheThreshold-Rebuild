/// <summary>
/// 파티가 공유하는 SP·OP의 획득, 상한, 소비를 관리한다.
/// 자원 처리 시점은 호출자가 정한다.
/// </summary>
public sealed class PartyResources
{
    public const int DefaultMaxSP = 7;
    public const int DefaultMaxOP = 6;
    public const int DefaultStartSP = 5;
    public const int DefaultStartOP = 2;
    public const int DefaultOPGainPerTurn = 1;

    public int MaxSP { get; }
    public int MaxOP { get; }
    public int OPGainPerTurn { get; }

    public int CurrentSP { get; private set; }
    public int CurrentOP { get; private set; }

    /// <param name="bonusStartSP">이벤트로 얻은 시작 SP 보너스</param>
    /// <param name="bonusStartOP">이벤트로 얻은 시작 OP 보너스</param>
    public PartyResources(
        int bonusStartSP = 0,
        int bonusStartOP = 0,
        int maxSP = DefaultMaxSP,
        int maxOP = DefaultMaxOP,
        int opGainPerTurn = DefaultOPGainPerTurn)
    {
        MaxSP = maxSP;
        MaxOP = maxOP;
        OPGainPerTurn = opGainPerTurn;

        CurrentSP = Clamp(DefaultStartSP + bonusStartSP, maxSP);
        CurrentOP = Clamp(DefaultStartOP + bonusStartOP, maxOP);
    }

    /// <summary>SP를 얻는다. 기본공격이 이 경로를 쓴다.</summary>
    public void GainSP(int amount)
    {
        if (amount <= 0) return;
        CurrentSP = Clamp(CurrentSP + amount, MaxSP);
    }

    public void GainOP(int amount)
    {
        if (amount <= 0) return;
        CurrentOP = Clamp(CurrentOP + amount, MaxOP);
    }

    /// <summary>턴 시작 시 OP를 자동으로 회복한다.</summary>
    public void GainOPOnTurnStart() => GainOP(OPGainPerTurn);

    /// <summary>SP가 충분하면 차감하고 true. 부족하면 아무것도 하지 않고 false.</summary>
    public bool TrySpendSP(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentSP < amount) return false;

        CurrentSP -= amount;
        return true;
    }

    public bool TrySpendOP(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentOP < amount) return false;

        CurrentOP -= amount;
        return true;
    }

    public bool CanSpendSP(int amount) => CurrentSP >= amount;
    public bool CanSpendOP(int amount) => CurrentOP >= amount;

    private static int Clamp(int value, int max)
    {
        if (value < 0) return 0;
        return value > max ? max : value;
    }

    public override string ToString() => $"SP {CurrentSP}/{MaxSP}, OP {CurrentOP}/{MaxOP}";
}
