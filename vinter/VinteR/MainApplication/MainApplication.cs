﻿using System;
using System.Linq;
using System.Net;
using VinteR.Configuration;
using VinteR.Input;
using VinteR.Model;
using VinteR.Rest;
using VinteR.Streaming;
using VinteR.ConnectionBroker;

namespace VinteR.MainApplication
{
    public class MainApplication : IMainApplication
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private enum ApplicationMode
        {
            Live,
            Play,
            Waiting
        }

        private readonly string _startMode;
        private readonly bool _startBroker;
        private readonly IRecordService _recordService;
        private readonly IPlaybackService _playbackService;
        private readonly IRestRouter[] _restRouters;
        private readonly IRestServer _restServer;
        private readonly IStreamingServer _streamingServer;
        private readonly IQueryService[] _queryServices;
        private readonly IConnectionBroker _connectionBroker;
        private ApplicationMode _currentMode;

        public MainApplication(IConfigurationService configurationService,
            IRecordService recordService,
            IPlaybackService playbackService,
            IRestServer restServer,
            IRestRouter[] routers,
            IStreamingServer streamingServer,
            IQueryService[] queryServices,
            IConnectionBroker connectionBroker)
        {
            _startMode = configurationService.GetConfiguration().StartMode;
            _startBroker = configurationService
                .GetConfiguration()
                .Adapters
                .First(a => a.AdapterType == HardwareSystems.Peer)
                .Enabled;
            _recordService = recordService;
            _playbackService = playbackService;
            _streamingServer = streamingServer;
            _restServer = restServer;
            _restRouters = routers;
            _queryServices = queryServices;
            _currentMode = ApplicationMode.Waiting;
            _connectionBroker = connectionBroker;
        }

        public void Start()
        {
            _restServer.Start();

            // start streaming server
            _streamingServer.Start();

            _playbackService.FrameAvailable += _streamingServer.Send;
            _recordService.FrameAvailable += _streamingServer.Send;
            _connectionBroker.OnPeerConnectionEstablished += HandleOnPeerConnectionEstablished;

            foreach (var restRouter in _restRouters)
            {
                restRouter.OnGetSessionCalled += HandleOnGetSessionCalled;
                restRouter.OnPlayCalled += HandleOnPlayCalled;
                restRouter.OnPausePlaybackCalled += HandleOnPausePlaybackCalled;
                restRouter.OnStopPlaybackCalled += HandleOnStopPlaybackCalled;
                restRouter.OnJumpPlaybackCalled += HandleOnJumpPlaybackCalled;
                restRouter.OnRecordSessionCalled += HandleOnRecordSessionCalled;
                restRouter.OnStopRecordCalled += HandleOnStopRecordCalled;
            }

            Logger.Info("_startMode: " + _startMode);
            switch (_startMode)
            {
                case "record":
                    StartRecord();
                    break;
                case "playback":
                    // nothing to to without session to play
                    break;
            }

            if (_startBroker)
            {
                _connectionBroker.Start();
            }
        }

        private void HandleOnPeerConnectionEstablished(Tuple<IPEndPoint, IPEndPoint> tx, Tuple<IPEndPoint, IPEndPoint> rx)
        {
            _streamingServer.AddReceiver(tx.Item2, tx.Item1);
            _recordService.StartPeerAdapter(rx.Item1, rx.Item2);
        }

        private Session HandleOnGetSessionCalled(string source, string sessionName, uint start, int end)
        {
            // If the session is already playing return it
            if (_playbackService.ContainsSession(source, sessionName, start, end))
            {
                return _playbackService.Session;
            }

            // otherwise load it from the query services
            var queryService = _queryServices.Where(qs => qs.GetStorageName() == source)
                .Select(qs => qs)
                .First();
            var session = queryService.GetSession(sessionName, start, end);
            return session;
        }

        private Session HandleOnStopRecordCalled()
        {
            return StopRecord();
        }

        private Session HandleOnRecordSessionCalled()
        {
            return StartRecord();
        }

        private void HandleOnJumpPlaybackCalled(object sender, uint millis)
        {
            JumpPlayback(millis);
        }

        private void HandleOnStopPlaybackCalled(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void HandleOnPausePlaybackCalled(object sender, EventArgs e)
        {
            PausePlayback();
        }

        private Session HandleOnPlayCalled(string source, string sessionName, uint start, int end)
        {
            return StartPlayback(source, sessionName, start, end);
        }

        public Session StartRecord()
        {
            Logger.Info("_currentMode: " + _currentMode);
            switch (_currentMode)
            {
                case ApplicationMode.Live:
                    Logger.Warn("Already recording");
                    break;
                case ApplicationMode.Play:
                    StopPlayback();
                    _recordService.Start();
                    break;
                case ApplicationMode.Waiting:
                    _recordService.Start();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentMode = ApplicationMode.Live;
            return _recordService.Session;
        }

        public Session StopRecord()
        {
            if (_currentMode == ApplicationMode.Live)
            {
                _recordService.Stop();
                _currentMode = ApplicationMode.Waiting;
                return _recordService.Session;
            }

            Logger.Warn("Application not in record");
            return null;
        }

        public Session StartPlayback(string source, string sessionName, uint start, int end)
        {
            Session session;
            switch (_currentMode)
            {
                case ApplicationMode.Live:
                    StopRecord();
                    session = _playbackService.Play(source, sessionName, start, end);
                    break;
                case ApplicationMode.Play:
                case ApplicationMode.Waiting:
                    session = _playbackService.Play(source, sessionName, start, end);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _currentMode = _playbackService.IsPlaying()
                ? ApplicationMode.Play
                : ApplicationMode.Waiting;
            return session;
        }

        public void PausePlayback()
        {
            if (_currentMode == ApplicationMode.Play)
            {
                _playbackService.Pause();
            }
            else
            {
                Logger.Warn("Application not in playback");
            }
        }

        public void StopPlayback()
        {
            if (_currentMode == ApplicationMode.Play)
            {
                _playbackService.Stop();
                _currentMode = ApplicationMode.Waiting;
            }
            else
            {
                Logger.Warn("Application not in playback");
            }
        }

        public void JumpPlayback(uint millis)
        {
            if (_currentMode == ApplicationMode.Play)
            {
                _playbackService.Jump(millis);
            }
            else
            {
                Logger.Warn("Application not in playback");
            }
        }

        public void Exit()
        {
            switch (_currentMode)
            {
                case ApplicationMode.Live:
                    _recordService.Stop();
                    break;
                case ApplicationMode.Play:
                    _playbackService.Stop();
                    break;
                case ApplicationMode.Waiting:
                    Logger.Info("All modes already stopped");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _restServer.Stop();
            _streamingServer.Stop();

            _playbackService.FrameAvailable -= _streamingServer.Send;
            _recordService.FrameAvailable -= _streamingServer.Send;

            Logger.Info("Application exited");
        }
    }
}