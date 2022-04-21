namespace SharpGen.Runtime.Diagnostics;

internal static class ObjectTrackerReadOnlyConfiguration
{
    public static readonly bool IsEnabled = Configuration.EnableObjectTracking;
    public static readonly bool IsReleaseOnFinalizerEnabled = Configuration.EnableReleaseOnFinalizer;
    public static readonly bool IsObjectTrackingThreadStatic = Configuration.UseThreadStaticObjectTracking;

    static ObjectTrackerReadOnlyConfiguration()
    {
        Configuration.ObjectTrackerImmutable = true;
    }
}