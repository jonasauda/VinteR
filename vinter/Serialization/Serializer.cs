using System.Linq;
using System;
using VinteR.Model.Gen;
using MocapFrame = VinteR.Model.MocapFrame;
using Session = VinteR.Model.Session;
using GenEBodyType = VinteR.Model.Gen.MocapFrame.Types.Body.Types.EBodyType;
using GenVector3 = VinteR.Model.Gen.MocapFrame.Types.Body.Types.Vector3;
using GenPoint = VinteR.Model.Gen.MocapFrame.Types.Body.Types.Point;
using VinteR.Model;
using System.Numerics;

namespace VinteR.Serialization
{
    public class Serializer : ISerializer
    {
        public void ToProtoBuf(MocapFrame frame, out Model.Gen.MocapFrame output)
        {
            // create mapping from MocapFrame to Gen.MocapFrame
            output = new Model.Gen.MocapFrame()
            {
                AdapterType = frame.AdapterType,
                ElapsedMillis = frame.ElapsedMillis,
                Gesture = frame.Gesture ?? "", // set default value otherwise serialization breaks
                Latency = frame.Latency,
                SourceId = frame.SourceId
            };

            foreach (var body in frame.Bodies)
            {
                var protoBody = new Model.Gen.MocapFrame.Types.Body()
                {
                    BodyType = body.GetBodyTypeProto(),
                    Rotation = body.Rotation.ToProto(),
                    SideType = body.GetSideTypeProto(),
                    Centroid = body.Centroid.ToProto(),
                    Name = body.Name ?? string.Empty
                };
                foreach (var point in body.Points)
                {
                    var protoPoint = new Model.Gen.MocapFrame.Types.Body.Types.Point()
                    {
                        Name = point.Name ?? "",
                        State = point.State ?? "",
                        Position = point.Position.ToProto()
                    };
                    protoBody.Points.Add(protoPoint);
                }

                output.Bodies.Add(protoBody);
            }
        }

        public void ToProtoBuf(Session session, out Model.Gen.Session output)
        {
            var mocapFrames = session.MocapFrames.Select(f =>
            {
                ToProtoBuf(f, out var generatedFrame);
                return generatedFrame;
            });
            ToProtoBuf(session, out SessionMetadata meta);
            output = new Model.Gen.Session { Meta = meta };
            output.Frames.AddRange(mocapFrames);
        }

        public void ToProtoBuf(Session session, out SessionMetadata output)
        {
            output = new SessionMetadata()
            {
                Name = session.Name,
                Duration = session.Duration,
                SessionStartMillis = session.Datetime.ToBinary()
            };
        }


        public void FromProtoBuf(Model.Gen.MocapFrame frame, out MocapFrame output)
        {
            output = new MocapFrame(frame.SourceId, frame.AdapterType)
            {
                ElapsedMillis = frame.ElapsedMillis,
                Gesture = frame.Gesture ?? "",
                Latency = frame.Latency,
            };

            foreach (var body in frame.Bodies)
            {
                var protoBody = new Body()
                {
                    BodyType = FromProto(body.BodyType),
                    Rotation = body.Rotation.FromProto(),
                    Side = ESideType.NoSide,
                    Centroid = FromProto(body.Centroid),
                    Name = body.Name ?? string.Empty
                };
                foreach (var protoPoint in body.Points)
                {
                    protoBody.Points.Add(FromProto(protoPoint));
                }

                output.Bodies.Add(protoBody);
            }

        }

        private Body.EBodyType FromProto(GenEBodyType type)
        {
            switch (type)
            {
                case GenEBodyType.Hand:
                    return Body.EBodyType.Hand;
                case GenEBodyType.Marker:
                    return Body.EBodyType.Marker;
                case GenEBodyType.MarkerSet:
                    return Body.EBodyType.MarkerSet;
                case GenEBodyType.RigidBody:
                    return Body.EBodyType.RigidBody;
                case GenEBodyType.Skeleton:
                    return Body.EBodyType.Skeleton;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Vector3 FromProto(GenVector3 vector)
        {
            return new Vector3()
            {
                X = vector.X,
                Y = vector.Y,
                Z = vector.Z,
            };
        }

        private Point FromProto(GenPoint point)
        {
            var p = new Point(point.Position.X, point.Position.Y, point.Position.Z);
            p.Name = point.Name;
            p.State = point.State;
            return p;
        }
    }
}