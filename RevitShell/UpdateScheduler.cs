using System.Threading;

namespace RevitShell;

/// <summary>Starts update checks without blocking Explorer commands.</summary>
internal static class UpdateScheduler
{
    private static int _checking;
    private static int _manualChecking;

    /// <summary>Starts one background STA manual check independently of automatic checks.</summary>
    public static void StartManualCheck()
    {
        try
        {
            if (Interlocked.CompareExchange(ref _manualChecking, 1, 0) != 0)
            {
                return;
            }

            var thread = new Thread(() =>
            {
                try
                {
                    RevitShellCompositionRoot.CheckForUpdates.Run(true);
                }
                catch
                {
                    // Update failures must never escape into Explorer.
                }
                finally
                {
                    Interlocked.Exchange(ref _manualChecking, 0);
                }
            })
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        catch
        {
            Interlocked.Exchange(ref _manualChecking, 0);
            // Thread creation or startup failures must never escape into Explorer.
        }
    }

    /// <summary>Starts one background STA check when no automatic check is running.</summary>
    public static void StartAutomaticCheck()
    {
        try
        {
            if (Interlocked.CompareExchange(ref _checking, 1, 0) != 0)
            {
                return;
            }

            var thread = new Thread(() =>
            {
                try
                {
                    RevitShellCompositionRoot.CheckForUpdates.Run(false);
                }
                catch
                {
                    // Update failures must never escape into Explorer.
                }
                finally
                {
                    Interlocked.Exchange(ref _checking, 0);
                }
            })
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        catch
        {
            Interlocked.Exchange(ref _checking, 0);
            // Thread creation or startup failures must never escape into Explorer.
        }
    }
}
