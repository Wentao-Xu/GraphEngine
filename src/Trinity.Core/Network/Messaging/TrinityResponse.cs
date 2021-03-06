// Graph Engine
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Lib;

namespace Trinity.Network.Messaging
{
    /// <summary>
    /// Represents a binary network response.
    /// Each TrinityResponse consists of an error code (4 bytes)
    /// and a payload.
    /// </summary>
    public unsafe sealed class TrinityResponse : IDisposable
    {
        static TrinityResponse()
        {
            InternalCalls.__init();
        }
        /// <summary>
        /// A pointer pointing to the underlying buffer.
        /// </summary>
        public byte* Buffer;
        /// <summary>
        /// The size of the underlying buffer.
        /// </summary>
        public int Size;
        /// <summary>
        /// The offset of the payload in the underlying buffer.
        /// </summary>
        public int Offset;

        internal TrinityResponse(int size)
        {
            Buffer = (byte*)CMemory.malloc((ulong)size);
            Size = size;
            Offset = TrinityProtocol.TrinityMsgHeader;
        }

        internal TrinityErrorCode ErrorCode
        {
            get
            {
                return (TrinityErrorCode)(*(int*)Buffer);
            }
        }

        // construct a TrinityResponse using a raw buffer
        internal TrinityResponse(byte* buf, int size)
        {
            Buffer = buf;
            Size = size;
            Offset = TrinityProtocol.TrinityMsgHeader;
        }

        internal TrinityResponse(TrinityMessage msg)
        {
            // !Note, in this case ErrorCode does not work.
            Buffer = msg.Buffer;
            Offset = TrinityMessage.Offset;
            Size = msg.Size;
        }

        /// <summary>
        /// Releases the unmanaged memory buffer used by the TrinityResponse.
        /// </summary>
        public void Dispose()
        {
            Memory.free(Buffer);
        }
    }
}
