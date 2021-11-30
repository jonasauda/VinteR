﻿using System.IO;
using VinteR.Model;

namespace VinteR.Serialization
{
    public interface ISerializer
    {
        /// <summary>
        /// Maps given mocap frame to a mocap frame defined inside the
        /// protobuf model.
        /// </summary>
        void ToProtoBuf(MocapFrame frame, out Model.Gen.MocapFrame output);

        /// <summary>
        /// Maps given session to a session defined inside the protobuf model
        /// including all frames that are given inside the session
        /// </summary>
        void ToProtoBuf(Session session, out Model.Gen.Session output);

        /// <summary>
        /// Maps given session to a session metadata object defined
        /// inside the protobuf model
        /// </summary>
        void ToProtoBuf(Session session, out Model.Gen.SessionMetadata output);
    }
}