using System;
using System.IO;

namespace QMC.Vision.Config
{
    /// <summary>Vision 전용 설정(Config) 저장 폴더. 기본값 D:\CDT-320\Config, 접근 시 폴더가 없으면 자동 생성.
    /// 핸들러(QMC.Common)와 공유하지 않는 Vision 전용 축 — vision.json / lfine_light.json / frame_specs.json 등이 사용한다.
    /// (핸들러 공유 config 는 종전대로 QMC.Common 측 exe\Config 를 사용하므로 서로 영향 없음.)</summary>
    public static class VisionPaths
    {
        private static string _configDir = @"D:\CDT-320\Config";

        /// <summary>Config 저장 폴더 절대경로. 접근 시 폴더가 없으면 자동 생성한다. 빈 값/공백 지정은 무시.</summary>
        public static string ConfigDir
        {
            get
            {
                try { Directory.CreateDirectory(_configDir); }
                catch { /* 권한/드라이브 부재 — 호출측 저장 시 처리 */ }
                return _configDir;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                _configDir = value;
            }
        }
    }
}
