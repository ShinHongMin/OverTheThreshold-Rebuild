using System;
using System.Text;

/// <summary>휴식 선택과 결과를 런에 보관한다. 화면 재표시로 효과나 치료 대상 추첨을 반복하지 않는다.</summary>
public sealed class RunRestSession
{
    private readonly RunState _run;
    private readonly IRandom _random;
    public event Action ActionSelected;
    public int NodeIndex { get; }
    public RestRules.Action? SelectedAction
    {
        get; private set;
    }

    public string Feedback { get; private set; } = "휴식·정비·응급 처치 중 한 가지를 선택할 수 있습니다.";

    public RunRestSession(RunState run, IRandom random)
    {
        if (run == null || run.IsFinished || run.PendingNode < 0 || run.Map[run.PendingNode].Type != NodeType.Rest)
            throw new ArgumentException("진입한 휴식 노드가 필요합니다.");
        _run = run;
        _random = random ?? throw new ArgumentNullException(nameof(random));
        NodeIndex = run.PendingNode;
    }

    internal void RestoreChoice(RestRules.Action action, string feedback)
    {
        RestRules.RestoreSelection(_run, action);
        SelectedAction = action;
        Feedback = feedback;
    }

    public bool CanSelect(RestRules.Action action)
        => !SelectedAction.HasValue && _run.PendingNode == NodeIndex && RestRules.CanApply(_run, action);

    public bool TrySelect(RestRules.Action action)
    {
        if (!CanSelect(action))
            return false;
        var beforeHP = new float[_run.Party.Count];
        var beforeER = new float[_run.Party.Count];
        for (int index = 0; index < _run.Party.Count; index++)
        {
            beforeHP[index] = _run.Party[index].CurrentHP;
            beforeER[index] = _run.Party[index].CurrentER;
        }
        if (!RestRules.TryApply(_run, action, _random))
            return false;
        SelectedAction = action;
        var summary = new StringBuilder();
        for (int index = 0; index < _run.Party.Count; index++)
        {
            var unit = _run.Party[index];
            bool changedHP = beforeHP[index] != unit.CurrentHP;
            bool changedER = beforeER[index] != unit.CurrentER;
            if (!changedHP && !changedER)
                continue;
            summary.Append(unit.Name).Append(": ");
            if (changedHP)
                summary.Append($"HP {beforeHP[index]:0.#} → {unit.CurrentHP:0.#}");
            if (changedER)
            {
                if (changedHP)
                    summary.Append(" / ");
                summary.Append($"ER {beforeER[index]:0.#} → {unit.CurrentER:0.#}");
                if (beforeER[index] >= Unit.EchoThreshold && !unit.IsEchoState)
                    summary.Append(" (ECHO 해제)");
            }
            summary.AppendLine();
        }
        Feedback = summary.Length > 0 ? summary.ToString().TrimEnd() : "휴식을 마쳤습니다. 변경된 수치는 없습니다.";
        ActionSelected?.Invoke();
        return true;
    }
}
