using System;

namespace Findary.Abstraction
{
    public class OperatingSystemWrapper : IOperatingSystem
    {
        public bool IsAndroid() => OperatingSystem.IsAndroid();

        public bool IsAndroidVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => OperatingSystem.IsAndroidVersionAtLeast(major, minor, build, revision);

        public bool IsBrowser() => OperatingSystem.IsBrowser();

        public bool IsFreeBSD() => OperatingSystem.IsFreeBSD();

        public bool IsFreeBSDVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => OperatingSystem.IsFreeBSDVersionAtLeast(major, minor, build, revision);

        public bool IsIOS() => OperatingSystem.IsIOS();

        public bool IsIOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => OperatingSystem.IsIOSVersionAtLeast(major, minor, build);

        public bool IsLinux() => OperatingSystem.IsLinux();

        public bool IsMacOS() => OperatingSystem.IsMacOS();

        public bool IsMacOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => OperatingSystem.IsMacOSVersionAtLeast(major, minor, build);

        public bool IsOSPlatform(string platform) => OperatingSystem.IsOSPlatform(platform);

        public bool IsOSPlatformVersionAtLeast(string platform, int major, int minor = 0, int build = 0, int revision = 0)
            => OperatingSystem.IsOSPlatformVersionAtLeast(platform, major, minor, build, revision);

        public bool IsTvOS() => OperatingSystem.IsTvOS();

        public bool IsTvOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => OperatingSystem.IsTvOSVersionAtLeast(major, minor, build);

        public bool IsWatchOS() => OperatingSystem.IsWatchOS();

        public bool IsWatchOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => OperatingSystem.IsWatchOSVersionAtLeast(major, minor, build);

        public bool IsWindows() => OperatingSystem.IsWindows();

        public bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => OperatingSystem.IsWindowsVersionAtLeast(major, minor, build, revision);
    }
}
