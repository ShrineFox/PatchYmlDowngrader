using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PatchYml
{
    internal class Program
    {
        public static List<Patch> patches;
        public static string ppuHash;

        static void Main(string[] args)
        {
            patches = new List<Patch>();
            ParseYML(args[0]);
            if (args[2] == "-str")
                ReplaceStrings();
            ConvertYML(args[1]);
        }

        public static byte[] StringToByteArray(String hex)
        {
            hex = hex.Replace("0x","");
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private static void ReplaceStrings()
        {
            for(int x = 0; x < patches.Count; x++)
            {
                List<string> patchLines = new List<string>();
                
                for (int y = 0; y < patches[x].PatchCode.Split('\n').Length; y++)
                {
                    string line = patches[x].PatchCode.Split('\n')[y];

                    if (line.Contains("utf8"))
                    {
                        uint offset = BitConverter.ToUInt32(StringToByteArray(line.Split(',')[1].Trim()), 0);
                        string utf8 = line.Split(',')[2].Replace("\"", "").Replace("]", "").Trim();
                        byte[] utf8Bytes = Encoding.ASCII.GetBytes(utf8);

                        List<byte> strBytes = new List<byte>();
                        for (int i = 0; i < utf8Bytes.Length; i++)
                        {
                            if (strBytes.Count == 4)
                            {
                                string offsetString = BitConverter.ToString(BitConverter.GetBytes(offset)).Replace("-", "");
                                string be32String = BitConverter.ToString(strBytes.ToArray()).Replace("-", "");
                                patchLines.Add($"  - [ be32, 0x{offsetString}, 0x{be32String} ] # {utf8}");
                                offset += 4;
                                strBytes = new List<byte>();
                            }

                            strBytes.Add(utf8Bytes[i]);
                        }

                        if (strBytes.Count > 0)
                        {
                            for (int i = strBytes.Count; i < 4; i++)
                                strBytes.Add(0x00);

                            string offsetString = BitConverter.ToString(BitConverter.GetBytes(offset)).Replace("-", "");
                            string be32String = BitConverter.ToString(strBytes.ToArray()).Replace("-", "");

                            patchLines.Add($"  - [ be32, 0x{offsetString}, 0x{be32String} ] # {utf8}");
                        }
                    }
                    else
                        patchLines.Add(line);
                }
                patches[x].PatchCode = string.Join("\n", patchLines);
            }
        }

        private static void ConvertYML(string format)
        {
            StringBuilder sb = new StringBuilder();

            if (format == "-new")
            {
                sb.Append("Version: 1.2");
                foreach (Patch patch in patches)
                    sb.Append($"\n\n{ppuHash}:" +
                        $"\n  {patch.Title}:" +
                        $"\n    Games:" +
                        $"\n      \"XRD664\":" +
                        $"\n        TEST00000: [ All ]" +
                        $"\n    Author: {patch.Author}" +
                        $"\n    Notes: {patch.Notes}" +
                        $"\n    Patch Version: {patch.PatchVersion}" +
                        $"\n    Patch:" +
                        $"\n    {patch.PatchCode.Replace("\n  ", "\n      ")}");
            }
            else
            {
                foreach (Patch patch in patches)
                {
                    string patchID = "p5_" + patch.Title.ToLower().Replace(" ", "_");
                    sb.Append($"# {patch.Title} v{patch.PatchVersion} by {patch.Author}" +
                        $"\n# {patch.Notes}" +
                        $"\n{patchID}: &{patchID}" +
                        $"\n{patch.PatchCode}");
                }
                sb.AppendLine($"{ppuHash}:");
                foreach (Patch patch in patches)
                {
                    string patchID = "p5_" + patch.Title.ToLower().Replace(" ", "_");
                    sb.Append($"\n- [ load, {patchID} ]");
                }
            }

            string text = sb.ToString();
            File.WriteAllText("patch.yml", text);
        }

        public static void ParseYML(string ymlPath)
        {
            List<string> ymlLines = File.ReadAllLines(ymlPath).ToList();

            for (int i = 0; i < ymlLines.Count(); i++)
            {
                // If line starts with "PPU-", begin reading patch
                if (ymlLines[i].StartsWith("PPU-"))
                {
                    ppuHash = ymlLines[i].Replace(":","");
                    // Continue serializing data until end of patch or yml file
                    var patch = new Patch();
                    int x = i;
                    x++;
                    patch.Title = ymlLines[x].TrimEnd(':').Trim();
                    x++;

                    while (x < ymlLines.Count() && !ymlLines[x].StartsWith("PPU-"))
                    {
                        x++;
                        switch (ymlLines[x])
                        {
                            case string s when !s.StartsWith(" "):
                                patch.Title = s.TrimEnd(':').Trim();
                                break;
                            case string s when s.StartsWith("    Author:"):
                                patch.Author = s.Replace("    Author:", "").Trim();
                                break;
                            case string s when s.StartsWith("    Notes:"):
                                patch.Notes = s.Replace("    Notes:", "").Trim();
                                break;
                            case string s when s.StartsWith("    Patch Version:"):
                                patch.PatchVersion = s.Replace("    Patch Version:", "").Trim();
                                break;
                            case string s when s.StartsWith("    Patch:"):
                                x++;
                                while (x < ymlLines.Count() && !ymlLines[x].StartsWith("PPU-"))
                                {
                                    patch.PatchCode += "  " + ymlLines[x].Trim() + "\n";
                                    x++;
                                }
                                i = x - 1;
                                break;
                        }
                    }

                    // Add serialized patch to patch list
                    patches.Add(patch);
                }
            }
        }
    }

    public class Patch
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Notes { get; set; } = "";
        public string PatchVersion { get; set; } = "1.0";
        public string PatchCode { get; set; } = "";
        public bool Enabled { get; set; } = false;
    }
}
