using Ninject.Modules;
using VinteR.Adapter;
using VinteR.Adapter.LeapMotion;
using VinteR.Adapter.Network;
using VinteR.Adapter.OptiTrack;
using VinteR.Adapter.Peer;
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

            Bind<IInputAdapter>().To<LeapMotionAdapter>().Named(HardwareSystems.LeapMotion);
            Bind<IInputAdapter>().To<OptiTrackAdapter>().Named(HardwareSystems.OptiTrack);
            Bind<IInputAdapter>().To<NetworkAdapter>().Named(HardwareSystems.HoloRoom);
            Bind<IInputAdapter>().To<PeerAdapter>().Named(HardwareSystems.Peer);

            Bind<ITransformator>().To<Transformator>();
            Bind<IAdapterTracker>().To<OptiTrackAdapterTracker>().InSingletonScope();
            Bind<IOptiTrackClient>().To<OptiTrackClient>().InSingletonScope();
            Bind<INetworkClient>().To<NetworkClient>().InSingletonScope();

            Bind<IDataMerger>().To<LeapMotionMerger>().Named(HardwareSystems.LeapMotion);
            Bind<IDataMerger>().To<KinectMerger>().Named(HardwareSystems.Kinect);
            Bind<IDataMerger>().To<OptiTrackMerger>().Named(HardwareSystems.OptiTrack);
            Bind<IDataMerger>().To<NetworkMerger>().Named(HardwareSystems.HoloRoom);
            Bind<IDataMerger>().To<PeerNetworkMerger>().Named(HardwareSystems.Peer);

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