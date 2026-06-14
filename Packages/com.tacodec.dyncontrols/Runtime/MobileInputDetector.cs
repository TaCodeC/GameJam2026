using System.Runtime.InteropServices;

namespace DynControls
{
    public static class MobileInputDetector
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int IsMobileBrowser();
#endif

        public static bool RequiresMobileControls()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#elif UNITY_WEBGL && !UNITY_EDITOR
            return IsMobileBrowser() == 1;
#else
            return false;
#endif
        }
    }
}
