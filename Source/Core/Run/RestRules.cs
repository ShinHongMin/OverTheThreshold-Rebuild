using System;

public static class RestRules
{
    public enum Action
    {
        Heal, Maintenance, CureEcho
    }
    /// <summary>저장된 회복 수치는 이미 복원되어 있다. 사용 여부만 복원하여 중복 적용을 막는다.</summary>
    public static void RestoreSelection(RunState run, Action action)
    {
        if (run == null || run.IsFinished || run.PendingNode < 0 || run.RestUsed ||
            run.Map[run.PendingNode].Type != NodeType.Rest || action < Action.Heal || action > Action.CureEcho)
            throw new ArgumentException("복원할 휴식 선택이 올바르지 않습니다.");
        run.RestUsed = true;
    }

    public static bool CanApply(RunState run, Action action)
    {
        if (run == null || run.IsFinished || run.PendingNode < 0 || run.RestUsed || run.Map[run.PendingNode].Type != NodeType.Rest)
            return false;
        if (action == Action.Heal || action == Action.Maintenance)
            return true;
        if (action == Action.CureEcho)
            foreach (var unit in run.Party)
                if (unit.IsEchoState)
                    return true;
        return false;
    }

    public static bool TryApply(RunState run, Action action, IRandom random)
    {
        if (!CanApply(run, action))
            return false;
        if (action == Action.CureEcho)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            int count = 0;
            foreach (var unit in run.Party)
                if (unit.IsEchoState)
                    count++;
            if (count == 0)
                return false;
            int selected = random.Range(0, count);
            foreach (var unit in run.Party)
                if (unit.IsEchoState && selected-- == 0)
                {
                    EchoRules.CureEcho(unit, null);
                    break;
                }
        }
        else if (action == Action.Heal)
        {
            foreach (var unit in run.Party)
                unit.Heal(unit.MaxHP * 0.2f);
        }
        else if (action == Action.Maintenance)
        {
            // 휴식은 ECHO 중 감소를 허용하는 전투 밖 경로다.
            foreach (var unit in run.Party)
                unit.SetER(unit.CurrentER - 30f);
        }
        else
            return false;
        run.RestUsed = true;
        return true;
    }
}
