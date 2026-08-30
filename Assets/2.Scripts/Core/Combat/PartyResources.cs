/// <summary>
/// 파티 공용 자원. SP와 OP는 개인 자원이 아니라 파티 전체가 함께 쓴다.
///
/// 기존 ResourceManager(싱글톤)를 대체한다. 싱글톤이 아니므로
/// 테스트에서 여러 개를 만들어도 서로 간섭하지 않는다.
///
/// 소비 판단은 이 클래스가 하고(TrySpend), 언제 부를지는 호출자가 정한다.
/// 기존 동작 기준으로 차감 시점은 "행동 예약 시점"이다.
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
