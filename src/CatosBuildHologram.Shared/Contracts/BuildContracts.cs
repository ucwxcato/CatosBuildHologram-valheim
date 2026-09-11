using System;

namespace CatosBuildHologram.Shared.Contracts
{
    public struct TransformData
    {
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public float RotationW;

        public bool IsFinite()
        {
            return IsFinite(PositionX) && IsFinite(PositionY) && IsFinite(PositionZ)
                && IsFinite(RotationX) && IsFinite(RotationY)
                && IsFinite(RotationZ) && IsFinite(RotationW);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class BlueprintRecord
    {
        public string BlueprintId { get; set; }
        public string OwnerPlayerId { get; set; }
        public string PieceTypeId { get; set; }
        public TransformData Transform { get; set; }
        public BuildState BuildState { get; set; }
        public SupportState SupportState { get; set; }
        public RejectionCode BlockReason { get; set; }
        public uint Revision { get; set; }
    }

    public sealed class BuildHologramPreview
    {
        public bool Active { get; set; }
        public string PreviewToken { get; set; }
        public string PieceTypeId { get; set; }
        public TransformData Transform { get; set; }
        public AuthorityMode AuthorityMode { get; set; }
        public NativePlacementState NativePlacementState { get; set; }
        public SupportState SupportState { get; set; }
        public BuildState BuildState { get; set; }
        public string BlueprintId { get; set; }
        public uint Revision { get; set; }
    }

    public sealed class BuildHologramStatus
    {
        public string BlueprintId { get; set; }
        public uint Revision { get; set; }
        public SupportState SupportState { get; set; }
        public BuildState BuildState { get; set; }
        public RejectionCode BlockReason { get; set; }
    }

    public sealed class ClientHello
    {
        public byte ProtocolMajor { get; set; }
        public string ClientBuildVersion { get; set; }
        public uint Capabilities { get; set; }
    }

    public sealed class ServerHello
    {
        public byte ProtocolMajor { get; set; }
        public AuthorityMode AuthorityMode { get; set; }
        public string ServerBuildVersion { get; set; }
        public string WorldSessionId { get; set; }
        public uint FeatureFlags { get; set; }
    }

    public sealed class ProtocolMessage
    {
        public byte ProtocolMajor { get; set; }
        public MessageType MessageType { get; set; }
        public string RequestId { get; set; }
        public uint ExpectedRevision { get; set; }
        public byte[] Payload { get; set; }
    }
}
