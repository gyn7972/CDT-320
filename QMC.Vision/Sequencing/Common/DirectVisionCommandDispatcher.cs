using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 모듈 명령(Grab/Match/Inspect/Train)을 공통 코어(<see cref="VisionCommandCore"/>)로 실행하는 디스패처.
    /// TCP 서버(<c>VisionTcpServer</c>)와 동일 구현을 공유한다 — 결과 문자열·mm변환·로그 일관.
    /// args 두번째 항목으로 chipUid 전달 가능(있으면 이미지/데이터 로그·자재추적 수행).
    /// </summary>
    public sealed class DirectVisionCommandDispatcher : IVisionCommandDispatcher
    {
        public string Execute(IVisionModule module, string cmd, string[] args)
        {
            if (module == null) return "fail:no module";
            var cfg = VisionConfigStore.Current;
            string id      = args != null && args.Length > 0 ? args[0] : string.Empty;
            string chipUid = args != null && args.Length > 1 ? args[1] : string.Empty;

            switch ((cmd ?? string.Empty).ToUpperInvariant())
            {
                case "GRAB":
                case "EXPOSE":  return VisionCommandCore.Grab(module);
                case "MATCH":   return VisionCommandCore.Match(module, cfg, id, chipUid);
                case "INSPECT": return VisionCommandCore.Inspect(module, cfg, id, chipUid);
                case "TRAIN":   return VisionCommandCore.Train(module, id);
                default:        return "fail:unknown command - " + cmd;
            }
        }
    }
}
