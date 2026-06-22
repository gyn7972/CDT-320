using System;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 현재 활성 레시피(<see cref="VisionMachineRecipe"/>) 전역 접근점.
    /// <para>정적 로그 저장부(<see cref="ImageLogSaver"/>/<see cref="DataLogSaver"/>)와
    /// 검사기(<see cref="PlacementGapInspector"/>)가 머신 참조 없이도 레시피별
    /// 로그 토글(<c>LogEnable</c>)과 이미지 저장 모드(<c>ImageSaveMode</c>)를 읽기 위한 단일 진입점이다.</para>
    /// <para>Form1 이 머신 생성 직후 <see cref="SetProvider"/> 로 <c>() =&gt; Machine.Recipe</c> 를 등록한다.
    /// 레시피 전환 시에도 항상 최신 인스턴스를 반환하도록 인스턴스를 캐시하지 않고 provider 를 매번 호출한다.</para>
    /// </summary>
    public static class ActiveRecipeContext
    {
        private static Func<VisionMachineRecipe> _provider;

        /// <summary>활성 레시피를 돌려주는 provider 등록. null 등록 시 해제.</summary>
        public static void SetProvider(Func<VisionMachineRecipe> provider) => _provider = provider;

        /// <summary>현재 활성 레시피. 미등록/예외 시 null(이 경우 호출측은 안전 기본값 = 로그 ON, ALL 저장 으로 동작).</summary>
        public static VisionMachineRecipe Current
        {
            get
            {
                try { return _provider != null ? _provider() : null; }
                catch { return null; }
            }
        }
    }
}
