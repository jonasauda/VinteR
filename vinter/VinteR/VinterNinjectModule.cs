using Ninject.Modules;
using VinteR.Adapter;
#if LEAP
using VinteR.Adapter.LeapMotion;
#endif
#if KINECT
using VinteR.Adapter.Kinect;
#endif
using VinteR.Adapter.Network;
#if OPTITRACK
using VinteR.Adapter.OptiTrack;
#endif
#if PEER
using VinteR.Adapter.Peer;
#endif
using VinteR.Configuration;
using VinteR.Datamerge;
using VinteR.Input;
using VinteR.MainApplication;
using VinteR.Mongo;
using VinteR.OutputAdapter;
using VinteR.OutputManager;
using VinteR.Rest;
using VinteR.Serialization;
using VinteR.Streaming;
using VinteR.Tracking;
using VinteR.Transform;
using VinteR.ConnectionBroker;

namespace VinteR
{
    public class VinterNinjectModule : NinjectModule
    {

        public override void Load()
        {
            Bind<IMainApplication>().To<MainApplication.MainApplication>().InSingletonScope();
            Bind<IRecordService>().To<RecordService>().InSingletonScope();
            Bind<IPlaybackService>().To<PlaybackService>().InSingletonScope();
            Bind<IConfigurationService>().To<VinterConfigurationService>().InSingletonScope();

#if LEAP
            Bind<IInputAdapter>().To<LeapMotionAdapter>().Named(HardwareSystems.LeapMotion);
#endif
#if KINECT
            Bind<IInputAdapter>().To<KinectAdapter>().Named(HardwareSystems.Kinect);
#endif
#if OPTITRACK
            Bind<IInputAdapter>().To<OptiTrackAdapter>().Named(HardwareSystems.OptiTrack);
#endif
#if HOLOROOM
            Bind<IInputAdapter>().To<NetworkAdapter>().Named(HardwareSystems.HoloRoom);
#endif
#if PEER
            Bind<IInputAdapter>().To<PeerAdapter>().Named(HardwareSystems.Peer);
#endif

            Bind<ITransformator>().To<Transformator>();
#if OPTITRACK
            Bind<IAdapterTracker>().To<OptiTrackAdapterTracker>().InSingletonScope();
            Bind<IOptiTrackClient>().To<OptiTrackClient>().InSingletonScope();
#endif
            Bind<INetworkClient>().To<NetworkClient>().InSingletonScope();

#if LEAP
            Bind<IDataMerger>().To<LeapMotionMerger>().Named(HardwareSystems.LeapMotion);
#endif
#if KINECT
            Bind<IDataMerger>().To<KinectMerger>().Named(HardwareSystems.Kinect);
#endif
#if OPTITRACK
            Bind<IDataMerger>().To<OptiTrackMerger>().Named(HardwareSystems.OptiTrack);
#endif
#if HOLOROOM
            Bind<IDataMerger>().To<NetworkMerger>().Named(HardwareSystems.HoloRoom);
#endif
#if PEER
            Bind<IDataMerger>().To<PeerNetworkMerger>().Named(HardwareSystems.Peer);
#endif

            Bind<IOutputManager>().To<OutputManager.OutputManager>().InThreadScope();
            Bind<IOutputAdapter>().To<ConsoleOutputAdapter>().InThreadScope();
            Bind<IOutputAdapter>().To<JsonFileOutputAdapter>().InSingletonScope();
            Bind<IOutputAdapter>().To<MongoOutputAdapter>().InSingletonScope();

            // bind network servers as singleton as multiple port bindings lead to application errors
            Bind<IStreamingServer>().To<UdpSender>().InSingletonScope();
            Bind<IRestServer>().To<VinterRestServer>().InSingletonScope();

            Bind<ISerializer>().To<Serializer>();
            Bind<ISessionNameGenerator>().To<SessionNameGenerator>();

            Bind<IQueryService>().To<MongoQueryService>();
            Bind<IQueryService>().To<JsonStorage>().Named("JsonStorage");

            Bind<IHttpResponseWriter>().To<HttpResponseWriter>();
            Bind<IRestRouter>().To<DefaultRouter>().InSingletonScope();
            Bind<IRestRouter>().To<SessionsRouter>().InSingletonScope();
            Bind<IRestRouter>().To<SessionRouter>().InSingletonScope();

            Bind<ISessionPlayer>().To<SessionPlayer>().InSingletonScope();
            Bind<IVinterMongoDBClient>().To<VinterMongoDBClient>().InSingletonScope();

            Bind<IConnectionBroker>().To<ConnectionBroker.ConnectionBroker>().InSingletonScope();
        }
    }
}