﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClientDBExtractor
{
    internal class KeyValueConfig
    {
        private readonly Dictionary<string, List<string>> Data = new Dictionary<string, List<string>>();

        public List<string> this[string key]
        {
            get { return Data[key]; }
        }

        public static KeyValueConfig ReadKeyValueConfig(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return ReadKeyValueConfig(sr);
            }
        }

        public static KeyValueConfig ReadKeyValueConfig(TextReader reader)
        {
            var result = new KeyValueConfig();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) // skip empty lines and comments
                    continue;

                string[] tokens = line.Split(new char[] {'='}, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length != 2)
                    throw new Exception("KeyValueConfig: tokens.Length != 2");

                var values = tokens[1].Trim().Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                var valuesList = values.ToList();
                result.Data.Add(tokens[0].Trim(), valuesList);
            }
            return result;
        }

        public static KeyValueConfig ReadVerBarConfig(Stream stream)
        {
            using (var sr = new StreamReader(stream))
                return ReadVerBarConfig(sr);
        }

        public static KeyValueConfig ReadVerBarConfig(TextReader reader)
        {
            var result = new KeyValueConfig();
            string line;

            int lineNum = 0;

            while ((line = reader.ReadLine()) != null)
            {
                if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) // skip empty lines and comments
                    continue;

                string[] tokens = line.Split(new char[] {'|'});

                if (lineNum == 0) // keys
                {
                    foreach (var token in tokens)
                    {
                        var tokens2 = token.Split(new char[] {'!'});
                        result.Data[tokens2[0]] = new List<string>();
                    }
                }
                else // values
                {
                    if (result.Data.Count != tokens.Length)
                        continue;

                    for (int i = 0; i < result.Data.Count; i++)
                        result.Data.ElementAt(i).Value.Add(tokens[i]);
                }

                lineNum++;
            }

            return result;
        }
    }

    internal class CASCConfig
    {
        KeyValueConfig _BuildInfo;
        KeyValueConfig _BuildConfig;
        KeyValueConfig _CDNConfig;

        public static CASCConfig LoadLocalStorageConfig(string basePath)
        {
            var config = new CASCConfig {OnlineMode = false, BasePath = basePath};
            
            string buildInfoPath = Path.Combine(basePath, ".build.info");

            using (Stream buildInfoStream = new FileStream(buildInfoPath, FileMode.Open))
            {
                config._BuildInfo = KeyValueConfig.ReadVerBarConfig(buildInfoStream);
            }

            string buildKey = config._BuildInfo["Build Key"][0];
            string buildCfgPath = Path.Combine(basePath, "Data\\config\\", buildKey.Substring(0, 2), buildKey.Substring(2, 2), buildKey);
            using (Stream stream = new FileStream(buildCfgPath, FileMode.Open))
            {
                config._BuildConfig = KeyValueConfig.ReadKeyValueConfig(stream);
            }

            string cdnKey = config._BuildInfo["CDN Key"][0];
            string cdnCfgPath = Path.Combine(basePath, "Data\\config\\", cdnKey.Substring(0, 2), cdnKey.Substring(2, 2), cdnKey);
            using (Stream stream = new FileStream(cdnCfgPath, FileMode.Open))
            {
                config._CDNConfig = KeyValueConfig.ReadKeyValueConfig(stream);
            }
            
            return config;
        }

        public string BasePath { get; private set; }

        public bool OnlineMode { get; private set; }

        public byte[] EncodingMD5
        {
            get { return _BuildConfig["encoding"][0].ToByteArray(); }
        }

        public byte[] EncodingKey
        {
            get { return _BuildConfig["encoding"][1].ToByteArray(); }
        }

        public byte[] RootMD5
        {
            get { return _BuildConfig["root"][0].ToByteArray(); }
        }

        public string CDNUrl
        {
            get
            {
                return String.Format("http://{0}{1}", _BuildInfo["CDN Hosts"][0], _BuildInfo["CDN Path"][0]);
            }
        }

        public List<string> Archives
        {
            get { return _CDNConfig["archives"]; }
        }
    }
}
