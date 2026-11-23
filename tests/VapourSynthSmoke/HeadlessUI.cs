#if !WINDOWS
// The encode path uses localization keys without an active WPF application.
namespace Cascade.Services
{
    internal static class LocalizationService
    {
        public static string GetString(string key) => key;
    }
}

namespace System.Windows.Media
{
    public abstract class ImageSource { }
}
#endif
