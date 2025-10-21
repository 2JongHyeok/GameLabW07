using UnityEngine;

// 재사용 가능한 업그레이드를 위한 인터페이스
// 이 인터페이스를 구현하면 비용만 지불하면 계속 사용 가능
public interface IReuse
{
    // 재사용 가능한 업그레이드인지 확인
    bool IsReusable { get; }
}