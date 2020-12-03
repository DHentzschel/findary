namespace Findary.Abstraction
{
    public interface IOperatingSystem
    {
        bool IsAndroid();

        bool IsAndroidVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0);

        bool IsBrowser();

        bool IsFreeBSD();

        bool IsFreeBSDVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0);

        bool IsIOS();

        bool IsIOSVersionAtLeast(int major, int minor = 0, int build = 0);

        bool IsLinux();

        bool IsMacOS();

        bool IsMacOSVersionAtLeast(int major, int minor = 0, int build = 0);

        bool IsOSPlatform(string platform);

        bool IsOSPlatformVersionAtLeast(string platform, int major, int minor = 0, int build = 0, int revision = 0);

        bool IsTvOS();

        bool IsTvOSVersionAtLeast(int major, int minor = 0, int build = 0);

        bool IsWatchOS();

        bool IsWatchOSVersionAtLeast(int major, int minor = 0, int build = 0);

        bool IsWindows();

        bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0);

        string ToString();
    }
}
