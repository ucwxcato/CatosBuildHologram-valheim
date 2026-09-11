using System;

namespace CatosBuildHologram.Shared.Protocol
{
    public static class RequestIds
    {
        public static string Create()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
