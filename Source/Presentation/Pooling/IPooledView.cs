/// <summary>재사용 시 이전 표시·구독·재생 상태를 정리한다.</summary>
public interface IPooledView
{
    void OnRent();
    void OnReturn();
}
