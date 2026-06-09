using System.Collections.Generic;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// 파라미터 공급자 — finder/inspector/카메라/조명/검사파라미터 등 도메인 객체가 구현.
    /// P2 ParameterStore 가 Register(provider) 로 디스크립터를 수집한다.
    /// </summary>
    public interface IParameterProvider
    {
        /// <summary>정규 타깃 id (디스크립터 Target 의 기본값).</summary>
        string ParameterTarget { get; }

        /// <summary>이 객체가 노출하는 파라미터 디스크립터(값=자기 속성/필드 바인딩).</summary>
        IEnumerable<ParameterDescriptor> DescribeParameters();
    }
}
