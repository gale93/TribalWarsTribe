using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TribalWarsTribe
{
    class BrowseWeb
    {
        private string world;

        List<Village> vCache = new List<Village>();
        List<Player> pCache = new List<Player>();
        public BrowseWeb(string _world)
        {
            world = _world;

            load();
        }

        private void loadFromWeb(string url, string filename)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, filename);
            }

            decompress(new FileInfo(filename));
        }

        private void load()
        {
            loadFromWeb($"http://{world}.tribalwars.net/map/village.txt.gz", "village.txt.gz");
            loadFromWeb($"http://{world}.tribalwars.net/map/player.txt.gz", "player.txt.gz");
        }

        private void decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        public Village GetVillageByCoord(string coords)
        {
            foreach (var v in vCache)
                if (v.Pos == coords)
                    return v;
            var xy = coords.Split('|');

            var lines = File.ReadLines("village.txt");
            foreach (string line in lines)
            {
                var csv = line.Split(',');

                if (csv[2] == xy[0] && csv[3] == xy[1])
                {
                    vCache.Add(new Village() { ID = int.Parse(csv[0]), Owner = int.Parse(csv[4]), Pos = coords, Name = csv[1]});

                    return vCache[vCache.Count - 1];
                }
            }

            return null;
        }

        public Player GetPlayerByID(int ID)
        {
            foreach (var p in pCache)
                if (p.ID == ID)
                    return p;

            var lines = File.ReadLines("player.txt");
            foreach (string line in lines)
            {
                var csv = line.Split(',');

                if (int.Parse(csv[0]) == ID)
                {
                    pCache.Add(new Player() { ID = ID, Name = csv[1], Points = int.Parse(csv[4])});

                    return pCache[pCache.Count - 1];
                }
            }
        
            return null;
        }

        public Player GetPlayerByName(string name)
        {
            foreach (var p in pCache)
                if (p.Name == name)
                    return p;

            var lines = File.ReadLines("player.txt");
            foreach (string line in lines)
            {
                var csv = line.Split(',');

                if (NormalizeString(csv[1]) == name)
                {
                    pCache.Add(new Player() { ID = int.Parse(csv[0]), Name = name, Points = int.Parse(csv[4]) });

                    return pCache[pCache.Count - 1];
                }
            }

            return null;
        }

        public static string NormalizeString(string str)
        {
            return System.Uri.UnescapeDataString(str).Replace('+', ' ');
        }
    }
}
