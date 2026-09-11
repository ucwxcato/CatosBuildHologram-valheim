using System;
using System.Text;
using CatosBuildHologram.Shared.Contracts;

namespace CatosBuildHologram.Shared.Protocol
{
    public static class ProtocolValidation
    {
        public static bool IsCompatibleMajor(byte major)
        {
            return major == ProtocolLimits.CurrentMajor;
        }

        public static bool IsBoundedRequestId(string value)
        {
            return IsBoundedUtf8(value, ProtocolLimits.MaxRequestIdBytes);
        }

        public static bool IsBoundedPieceTypeId(string value)
        {
            return IsBoundedUtf8(value, ProtocolLimits.MaxPieceTypeBytes);
        }

        public static bool IsValidTransform(TransformData transform)
        {
            return transform.IsFinite();
        }

        public static bool IsBoundedPayload(byte[] payload)
        {
            return payload == null || payload.Length <= ProtocolLimits.MaxMessageBytes;
        }

        public static bool IsValidBlueprint(BlueprintRecord record)
        {
            return record != null
                && IsBoundedRequestId(record.BlueprintId)
                && IsBoundedRequestId(record.OwnerPlayerId)
                && IsBoundedPieceTypeId(record.PieceTypeId)
                && IsValidTransform(record.Transform)
                && record.Revision > 0;
        }

        private static bool IsBoundedUtf8(string value, int maxBytes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Encoding.UTF8.GetByteCount(value) <= maxBytes;
        }
    }
}
