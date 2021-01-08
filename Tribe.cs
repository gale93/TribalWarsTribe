using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TribalWarsTribe
{
    class Tribe
    {
        public string world_link;
        public string PacketSize;

        private List<VillageSupport> PacketsRequired;

        public Dictionary<string, List<VillageSupport>> Players;

        private string current_tag = "";
        private List<string> buffer;

        public BrowseWeb bweb { get; private set; }
        bool realoadbweb = true;
        public Tribe()
        {
            Players = new Dictionary<string, List<VillageSupport>>();
            buffer = new List<string>();
            PacketsRequired = new List<VillageSupport>();
        }

        private bool isTag(string str)
        {
            return str.Contains("[") && str.Contains("]");
        }

        private string retrieveTag(string str)
        {
            int start = str.IndexOf('[') + 1;
            int end = str.IndexOf(']');

            return str.Substring(start, end - start);
        }

        public void LoadData(string data, bool reloadOnline)
        {
            realoadbweb = reloadOnline;
            using (var reader = new StringReader(data))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    if (isTag(line))
                    {
                        analyzeChunk();

                        current_tag = retrieveTag(line.ToLower());
                        buffer.Clear();
                    }
                    else if (line != string.Empty)
                        buffer.Add(line);
                }
            }

            analyzeChunk();
        }

        public void ReloadBweb()
        {
            bweb = new BrowseWeb(world_link);
        }

        private void analyzeChunk()
        {
            if (buffer.Count() == 0 || current_tag == "")
                return;

            switch (current_tag)
            {
                case "world_link":
                    world_link = buffer[0];
                    if (realoadbweb)
                        ReloadBweb();

                    break;
                case "packs_size":
                    PacketSize = buffer[0];
                    break;
                case "packs_required":
                    foreach (string s in buffer)
                    {
                        string[] values = s.Split(',');

                        AddRequiredSupports(bweb.GetVillageByCoord(values[0]), int.Parse(values[1]));
                    }

                    break;
                case "user":
                    string name = buffer[0];
                    buffer.RemoveAt(0);

                    Player p = bweb.GetPlayerByName(name);


                    foreach (string s in buffer)
                    {
                        string[] values = s.Split(',');
                        AddPlayerSupports(p, bweb.GetVillageByCoord(values[0]), int.Parse(values[1]));

                        //Players[name].Add(new VillageSupport() { Village = values[0], TotalPackets = int.Parse(values[1]) });
                    }

                    break;
            }
        }


        public List<VillageSupport> GetCalculatedVillageSupportList()
        {
            var lst = new List<VillageSupport>(PacketsRequired);

            foreach (var p in Players)
                foreach (var vs in p.Value)
                    foreach(var v in lst)
                        if (v.Village == vs.Village)
                        {
                            v.currentPackets += vs.TotalPackets;
                            continue;
                        }

            foreach (var v in lst)
            {
                double ratio = (double)v.TotalPackets / (double)v.currentPackets;

                if (ratio <= 1)
                    v.ColorSet = "Green";
                else if (ratio <= 2)
                    v.ColorSet = "Orange";
                else
                    v.ColorSet = "Red";
            }


            return lst;
        }

        public string GetSupportsByCoords(string coords)
        {
            string supports = "";
            foreach (var player in Players)
                foreach (var supp in player.Value)
                    if (supp.Village == coords)
                        supports += $"{player.Key} ({supp.TotalPackets}), ";

            return supports;
        }


        public void AddPlayerSupports(Player p, Village v, int packets)
        {
            if (!Players.ContainsKey(p.Name))
                Players[p.Name] = new List<VillageSupport>();

            if (packets == 0)
            {
                var itemToRemove = Players[p.Name].SingleOrDefault(r => r.Village == v.Pos);
                if (itemToRemove != null)
                    Players[p.Name].Remove(itemToRemove);

                return;
            }

            foreach (var supp in Players[p.Name])
                if (supp.Village == v.Pos)
                {
                    supp.TotalPackets = packets;
                    return;
                }

            Players[p.Name].Add(new VillageSupport() { Village = v.Pos, TotalPackets = packets });
        }

        public void AddRequiredSupports(Village v, int packets)
        {
            if (packets == 0)
            {
                var itemToRemove = PacketsRequired.SingleOrDefault(r => r.Village == v.Pos);
                if (itemToRemove != null)
                    PacketsRequired.Remove(itemToRemove);

                return;
            }

            foreach (var pr in PacketsRequired)
                if (pr.Village == v.Pos)
                {
                    pr.TotalPackets = packets;
                    return;
                }
                    
            PacketsRequired.Add(new VillageSupport() { Village = v.Pos, TotalPackets = packets });
        }

        public string GetFile()
        {
            string file = "";
            Action<string> write = str =>
            {
                file += str + Environment.NewLine;
            };

            write("[world_link]");
            write(world_link);
            
            write("[packs_size]");
            write(PacketSize);
            
            write("[packs_required]");

            foreach(var p in PacketsRequired)
                write(p.Village + "," + p.TotalPackets);


            foreach (var p in Players)
            {
                write("[user]");
                write(p.Key);

                foreach (var supp in Players[p.Key])
                    write(supp.Village + "," + supp.TotalPackets);
            }

            return file;
        }
    }
}
