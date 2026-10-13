/// <summary>
/// 전투에 참여하는 유닛 하나. 아군/적군 구분은 CombatContext가 갖는다.
/// </summary>
public sealed class Unit
{
    /// <summary>이 값에 도달하면 ECHO 상태가 된다.</summary>
    public const float EchoThreshold = 100f;

    /// <summary>ER 상한.</summary>
    public const float MaxER = 100f;

    public string Name { get; }
    public CharacterJob Job { get; }
    /// <summary>캐릭터 자원(SP·OP·ER) 사용 여부. 몬스터는 false로 생성한다.</summary>
    public bool UsesResources { get; }
    public UnitStats Stats { get; }

    public float CurrentHP { get; private set; }

    /// <summary>남은 실드량. 피해를 HP보다 먼저 흡수한다.</summary>
    public float CurrentShield { get; private set; }

    /// <summary>실드가 남아 있는 턴 수. 감소 처리는 TurnLoop이 담당한다.</summary>
    public int ShieldDuration { get; private set; }

    /// <summary>공명 수치 0~100.</summary>
    public float CurrentER { get; private set; }

    public float MaxHP => Stats.Get(StatType.MaxHP);
    public bool IsAlive => CurrentHP > 0f;
    public bool HasShield => CurrentShield > 0f;

    /// <summary>
    /// ER로 판정하는 ECHO 상태. 자원을 사용하지 않는 몬스터는 제외한다.
    /// </summary>
    public bool IsEchoState => UsesResources && CurrentER >= EchoThreshold;

    /// <summary>ECHO 상태에서도 스스로 행동을 정할 수 있는가.</summary>
    public bool KeepsControlInEcho => Job == CharacterJob.Resonance;

    public Unit(string name, UnitStats stats, CharacterJob job = CharacterJob.Vanguard, bool usesResources = true)
    {
        Name = name;
        Job = job;
        UsesResources = usesResources;
        Stats = stats;
        CurrentHP = stats.Get(StatType.MaxHP);
    }

    /// <summary>
    /// 피해를 적용한다. 실드가 먼저 흡수하고 남은 만큼만 체력에서 깎인다.
    /// 결과 이벤트는 호출자가 반환된 DamageBreakdown으로 생성한다.
    /// </summary>
    internal DamageBreakdown ApplyDamage(float amount)
    {
        if (amount <= 0f) return DamageBreakdown.None;

        float absorbed = 0f;

        if (CurrentShield > 0f)
        {
            absorbed = CurrentShield < amount ? CurrentShield : amount;
            CurrentShield -= absorbed;
            amount -= absorbed;

            if (CurrentShield <= 0f)
            {
                CurrentShield = 0f;
                ShieldDuration = 0;
            }
        }

        float hpDamage = 0f;

        if (amount > 0f)
        {
            float before = CurrentHP;
            CurrentHP = before - amount;
            if (CurrentHP < 0f) CurrentHP = 0f;
            hpDamage = before - CurrentHP;
        }

        return new DamageBreakdown(absorbed, hpDamage);
    }

    /// <summary>
    /// 체력을 회복시키고 실제로 회복된 양을 반환한다.
    /// 회복 상한은 스탯 보정이 반영된 MaxHP다.
    /// </summary>
    internal float Heal(float amount)
    {
        if (amount <= 0f || !IsAlive) return 0f;

        float before = CurrentHP;
        float cap = MaxHP;

        CurrentHP += amount;
        if (CurrentHP > cap) CurrentHP = cap;

        return CurrentHP - before;
    }

    /// <summary>
    /// 실드를 부여하고 실제로 늘어난 양을 반환한다.
    /// 최대 체력이 상한이며, 지속 턴은 더 긴 쪽으로 갱신된다.
    /// </summary>
    internal float AddShield(float amount, int duration)
    {
        if (amount <= 0f || duration <= 0) return 0f;

        float before = CurrentShield;

        CurrentShield += amount;

        float cap = MaxHP;
        if (CurrentShield > cap) CurrentShield = cap;

        if (duration > ShieldDuration) ShieldDuration = duration;

        return CurrentShield - before;
    }

    /// <summary>ER을 설정한다. 0~100으로 제한된다. 실제로 변한 양을 반환.</summary>
    internal float SetER(float value)
    {
        if (!UsesResources) return 0f;
        float before = CurrentER;

        if (value < 0f) value = 0f;
        else if (value > MaxER) value = MaxER;

        CurrentER = value;
        return CurrentER - before;
    }

    internal void RestorePersistentState(float hp, float er)
    {
        if (float.IsNaN(hp) || float.IsInfinity(hp) || hp < 0f || hp > MaxHP ||
            float.IsNaN(er) || float.IsInfinity(er) || er < 0f || er > MaxER)
            throw new System.ArgumentException("저장된 HP/ER이 허용 범위를 벗어났습니다.");
        ResetCombatState();
        CurrentHP = hp;
        CurrentER = er;
    }

    internal void ClearShield()
    {
        CurrentShield = 0f;
        ShieldDuration = 0;
    }

    public void ResetCombatState()
    {
        Stats.ClearBuffs();
        ClearShield();
        ClampHPToMaximum();
    }

    internal void ClampHPToMaximum() { if (CurrentHP > MaxHP) CurrentHP = MaxHP; }

    /// <summary>실드 지속 턴을 1 줄인다. 만료되면 실드가 사라지고 true를 반환.</summary>
    internal bool TickShieldDuration()
    {
        if (CurrentShield <= 0f) return false;

        ShieldDuration--;

        if (ShieldDuration <= 0)
        {
            ShieldDuration = 0;
            CurrentShield = 0f;
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        string shield = HasShield ? $" 실드 {CurrentShield:0.##}({ShieldDuration}턴)" : "";
        string echo = IsEchoState ? " [ECHO]" : "";
        return $"{Name} HP {CurrentHP:0.##}/{MaxHP:0.##} ER {CurrentER:0.#}{shield}{echo}";
    }
}
