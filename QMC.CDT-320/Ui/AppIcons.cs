using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui
{
    // To do: [앱 아이콘] 창 타이틀바/작업표시줄 아이콘 적용 헬퍼 (2026-08-05 지시).
    //  - 메인 창: csproj EmbeddedResource로 임베드한 Logo.ico를 LogicalName으로 읽는다(멀티사이즈·안정 핸들). 실패 시 exe 아이콘으로 폴백.
    //  - 팝업(조그/포지션): csproj EmbeddedResource로 임베드한 ico를 LogicalName으로 읽는다.
    //  아이콘은 표시 실패해도 기능에 영향이 없어야 하므로 모든 실패를 조용히 삼킨다.
    internal static class AppIcons
    {
        /// <summary>임베드한 Logo.ico(멀티사이즈)를 창 아이콘으로 적용합니다. 없으면 exe 아이콘으로 폴백합니다.</summary>
        public static void ApplyMainIcon(Form form)
        {
            try
            {
                if (form == null)
                    return;

                // 임베드한 Logo.ico를 우선 사용한다. 16~256 멀티사이즈를 모두 담고 있어
                // 타이틀바/작업표시줄이 DPI에 맞는 크기를 고르고, 소유 핸들이라 안정적이다.
                Assembly assembly = typeof(AppIcons).Assembly;
                using (Stream stream = assembly.GetManifestResourceStream("QMC.CDT_320.Resources.Logo.ico"))
                {
                    if (stream != null)
                    {
                        form.Icon = new Icon(stream);
                        return;
                    }
                }

                // 폴백: 임베드 리소스가 없으면 exe에 박힌 애플리케이션 아이콘(32x32 단일)을 꺼내 쓴다.
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                    form.Icon = icon;
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>임베드된 ico 리소스를 창에 적용합니다. 없으면 메인 로고로 폴백합니다.</summary>
        /// <param name="iconFileName">예: "axis-jog.ico" (LogicalName 접두어는 내부에서 붙인다)</param>
        public static void ApplyEmbeddedIcon(Form form, string iconFileName)
        {
            try
            {
                if (form == null || string.IsNullOrEmpty(iconFileName))
                    return;

                Assembly assembly = typeof(AppIcons).Assembly;
                string resourceName = "QMC.CDT_320.Resources." + iconFileName;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        form.Icon = new Icon(stream);
                        return;
                    }
                }

                ApplyMainIcon(form);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
