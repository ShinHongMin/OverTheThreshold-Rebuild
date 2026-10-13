using System.Runtime.CompilerServices;

// Unit.ApplyDamage 등 internal 멤버를 테스트 어셈블리에서 호출하기 위한 설정.
// 캡슐화는 유지하면서 테스트만 예외적으로 통과시킨다.
// 문자열은 Tests/OTT.Core.Tests.asmdef 의 name 과 반드시 일치해야 한다.
[assembly: InternalsVisibleTo("OTT.Core.Tests")]
