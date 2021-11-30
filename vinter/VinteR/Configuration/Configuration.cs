﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VinteR.Configuration
{
    [JsonObject]
    public class Configuration
    {
        [JsonProperty("start.mode")] public string StartMode { get; set; }
        [JsonProperty("home.dir")] public string HomeDir { get; set; }

        [JsonProperty("rest")] public Rest Rest { get; set; }

        [JsonProperty("udp.server.port")] public int UdpServerPort { get; set; }

        [JsonProperty("udp.receivers")] public IList<UdpReceiver> UdpReceivers { get; set; }
        [JsonProperty("mongodb")] public MongoDB Mongo { get; set; }
        [JsonProperty("adapters")] public IList<Adapter> Adapters { get; set; }
        [JsonProperty("jsonLoggerEnable")] public bool JsonLoggerEnable { get; set; }
    }

    public class Rest
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }
        [JsonProperty("host")] public string Host { get; set; }
        [JsonProperty("port")] public int Port { get; set; }
    }

    public class MongoDB
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }
        [JsonProperty("write")] public bool Write { get; set; }
        [JsonProperty("domain")] public string Domain { get; set; }
        [JsonProperty("user")] public string User { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonProperty("database")] public string Database { get; set; }
        [JsonProperty("port")] public int Port { get; set; }
        [JsonProperty("bufferSize")] public int MongoBufferSize { get; set; }
    }

    public class UdpReceiver
    {
        [JsonProperty("ip")] public string Ip { get; set; }
        [JsonProperty("port")] public int Port { get; set; }
        [JsonProperty("hrri")] public string Hrri { get; set; }
    }

    public class Adapter
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("adaptertype")] public string AdapterType { get; set; }
        [JsonProperty("framedroprate")] public int FramedropRate { get; set; }
        [JsonProperty("client.port")] public int ClientPort { get; set; }
        [JsonExtensionData] private readonly IDictionary<string, JToken> _additionalSettings;

        // holoroom props
        [JsonProperty("hrri")] public string Hrri { get; set; }

        // peer props
        [JsonProperty("keepalive")] public bool KeepAlive { get; set; }
        [JsonProperty("client.tx.port")] public int ClientTxPort { get; set; }
        [JsonProperty("broker.address")] public string BrokerHostName { get; set; }
        [JsonProperty("broker.port")] public int BrokerPort { get; set; }

        public IDictionary<string, JToken> AdditionalSettings => _additionalSettings;

        // kinect props
        public string DataDir { get; set; }
        public bool ColorStreamEnabled { get; set; }
        public bool ColorStreamFlush { get; set; }
        public bool DepthStreamEnabled { get; set; }
        public bool DepthStreamFlush { get; set; }
        public bool SkeletonStreamFlush { get; set; }
        public string ColorStreamFlushDir { get; set; }
        public string DepthStreamFlushDir { get; set; }
        public string SkeletonStreamFlushDir { get; set; }
        public int ColorStreamFlushSize { get; set; }
        public int DepthStreamFlushSize { get; set; }
        public int SkeletonStreamFlushSize { get; set; }
        public bool SkeletonTrackingStateFilter { get; set; }

        // optitrack props
        public string ServerIp { get; set; }
        public string ClientIp { get; set; }
        public string ConnectionType { get; set; }

        public Adapter()
        {
            _additionalSettings = new Dictionary<string, JToken>();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            var setting = _additionalSettings.GetValueOrDefault("colorStream.enabled", null);
            ColorStreamEnabled = setting != null && bool.TrueString.Equals((string)setting);
            setting = _additionalSettings.GetValueOrDefault("colorStream.enabled", null);
            ColorStreamEnabled = setting != null && bool.TrueString.Equals((string)setting);
            setting = _additionalSettings.GetValueOrDefault("colorStream.flush", null);
            ColorStreamFlush = setting != null && bool.TrueString.Equals((string)setting);
            setting = _additionalSettings.GetValueOrDefault("depthStream.enabled", null);
            DepthStreamEnabled = setting != null && bool.TrueString.Equals((string)setting);
            setting = _additionalSettings.GetValueOrDefault("depthStream.flush", null);
            DepthStreamFlush = setting != null && bool.TrueString.Equals((string)setting);
            setting = _additionalSettings.GetValueOrDefault("skeletonStream.flush", null);
            SkeletonStreamFlush = setting != null && bool.TrueString.Equals((string)setting);
            setting = _additionalSettings.GetValueOrDefault("data.dir", "KinectData");
            DataDir = (string)setting;
            setting = _additionalSettings.GetValueOrDefault("colorStream.flush.dirname", "ColorStreamData");
            ColorStreamFlushDir = (string)setting;
            setting = _additionalSettings.GetValueOrDefault("depthStream.flush.dirname", "DepthStreamData");
            DepthStreamFlushDir = (string)setting;
            setting = _additionalSettings.GetValueOrDefault("skeletonStream.flush.dirname", "SkeletonStreamData");
            SkeletonStreamFlushDir = (string)setting;
            setting = _additionalSettings.GetValueOrDefault("skeletonStream.flushSize", 200);
            SkeletonStreamFlushSize = (int)setting;
            setting = _additionalSettings.GetValueOrDefault("colorStream.flushSize", 60);
            ColorStreamFlushSize = (int)setting;
            setting = _additionalSettings.GetValueOrDefault("depthStream.flushSize", 30);
            DepthStreamFlushSize = (int)setting;
            setting = _additionalSettings.GetValueOrDefault("skeleton.TrackingFilter.enabled", false);
            SkeletonTrackingStateFilter = setting != null && bool.TrueString.Equals((string)setting);


            setting = _additionalSettings.GetValueOrDefault("server.ip", null);
            ServerIp = setting != null ? (string)setting : null;
            setting = _additionalSettings.GetValueOrDefault("client.ip", null);
            ClientIp = setting != null ? (string)setting : null;
            setting = _additionalSettings.GetValueOrDefault("connection.type", null);
            ConnectionType = setting != null ? (string)setting : null;
        }
    }

    public static class DictionaryExtension
    {
        public static TV GetValueOrDefault<TK, TV>(this IDictionary<TK, TV> @this, TK key, TV @default)
        {
            return @this.ContainsKey(key) ? @this[key] : @default;
        }
    }
}