#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;

/// <summary>기본 Assembly-CSharp의 SO/App 경로를 Unity 안에서 검증한다.</summary>
public class UnityDataTests
{
    private static void Validate(string method)
    {
        var type = Assembly.Load("Assembly-CSharp-Editor").GetType("ProjectValidation", true);
        try { type.GetMethod(method, BindingFlags.Static | BindingFlags.Public).Invoke(null, null); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
    [Test] public void 실제_SO의_회복과_보스_사이클() => Validate("VerifyData");
    [Test] public void 실제_Unity_풀_재사용과_중복_반환() => Validate("VerifyPool");
    [Test] public void 런_조립과_전투_복귀_난수_분리() => Validate("VerifyRun");
}
#endif
