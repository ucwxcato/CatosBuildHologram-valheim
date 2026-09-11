namespace CatosBuildHologram.Shared.Protocol
{
    public static class ProtocolLimits
    {
        public const byte CurrentMajor = 1;
        public const int MaxMessageBytes = 16 * 1024;
        public const int MaxBlueprintsPerPage = 128;
        public const int MaxBlueprintsPerOwner = 512;
        public const int MaxBlueprintsPerWorld = 4096;
        public const int MaxReasonBytes = 160;
        public const int MaxPieceTypeBytes = 128;
        public const int MaxPlanGroupBytes = 64;
        public const int MaxRequestIdBytes = 64;
        public const int MaxRequestRatePerSecond = 8;
        public const int RequestBurst = 16;

    }
}
