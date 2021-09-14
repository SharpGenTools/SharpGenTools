namespace SharpGen.Runtime
{
    /// <summary>
    ///     Represents the trust level of an activatable class.
    /// </summary>
    public enum TrustLevel
    {
        /// <summary>
        ///     The component has access to resources that are not protected.
        /// </summary>
        BaseTrust = 0,

        /// <summary>
        ///     The component has access to resources requested in the app manifest and approved by the user.
        /// </summary>
        PartialTrust = 1,

        /// <summary>
        ///     The component requires the full privileges of the user.
        /// </summary>
        FullTrust = 2
    }
}