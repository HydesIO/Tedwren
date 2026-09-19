using UIKit;

namespace Tedwren.Mobile;

/// <summary>The iOS process entry point.</summary>
public class Program
{
    /// <summary>Starts the UIApplication with the Tedwren <see cref="AppDelegate"/>.</summary>
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
