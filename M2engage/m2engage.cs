using GMWare.M2.Psb;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLunarUI.M2engage
{
    public static class m2engage
    {
        public static string FindPsbKey(string filePath)
        {
            string key = string.Empty;
            BinaryReader reader = new BinaryReader(File.OpenRead(filePath));

            for (int i = 0; i < reader.BaseStream.Length; i += 1024)
            {
                byte[] dataBlock = new byte[1024];
                reader.Read(dataBlock, 0, 1024);

                string blockText = Encoding.Default.GetString(dataBlock);
                if (blockText.Contains("getExistFileDirInMountArchive"))
                {
                    key = blockText.Substring(blockText.IndexOf("getExistFileDirInMountArchive") + 32, 13);
                    break;
                }
            }
            return key;
        }

        public static void MigrateSaves(int sourceSize, int destinationSize, string lunarPath = null)
        {
            using (ScpClient scp = new ScpClient("169.254.215.100", "root", "5A7213"))
            {
                scp.Connect();

                MemoryStream settingsDataStream = new MemoryStream();
                scp.Download("/usr/game/save/data_008_0000.bin", settingsDataStream);

                MemoryStream settingsMetaStream = new MemoryStream();
                scp.Download("/usr/game/save/meta_008_0000.bin", settingsMetaStream);

                byte[] settingsData = new byte[settingsDataStream.Length];
                settingsDataStream.Position = 0;
                settingsDataStream.Read(settingsData, 0, settingsData.Length);
                SystemData originalSave = SystemData.FromByteArray(settingsData, sourceSize);
                SystemData migratedSave = new SystemData(destinationSize);

                migratedSave.Base_Settings = originalSave.Base_Settings;
                migratedSave.Work_Trial = originalSave.Work_Trial;
                int savesToCopy = Math.Min(sourceSize, destinationSize);
                for (int i = 0; i < savesToCopy; i++)
                {
                    migratedSave.Setting_Games[i] = originalSave.Setting_Games[i];
                    migratedSave.Sram_Data[i] = originalSave.Sram_Data[i];
                }

                byte[] migratedData = migratedSave.ToByteArray();

                using (MemoryStream saveFile = new MemoryStream(migratedData))
                {
                    scp.Upload(saveFile, "/usr/game/save/data_008_0000.bin");
                    if (!string.IsNullOrEmpty(lunarPath))
                    {
                        File.WriteAllBytes($@"{lunarPath}\data_008_0000.bin", migratedData);
                    }
                }

                settingsMetaStream.Position = 0;
                using (PsbReader psbReader = new PsbReader(settingsMetaStream))
                {
                    JToken meta = psbReader.Root;
                    meta["FileSize"] = migratedData.Length;
                    meta["OriginalSize"] = migratedData.Length;
                    (meta["Digest"] as JStream).BinaryData = MD5.Create().ComputeHash(migratedData);

                    PsbWriter psbWriter = new PsbWriter(meta, null);
                    psbWriter.Version = psbReader.Version;

                    using (MemoryStream metaFile = new MemoryStream())
                    {
                        psbWriter.Write(metaFile, null);
                        metaFile.Position = 0;
                        scp.Upload(metaFile, "/usr/game/save/meta_008_0000.bin");

                        if (!string.IsNullOrEmpty(lunarPath))
                        {
                            byte[] metaData = new byte[metaFile.Length];
                            metaFile.Position = 0;
                            metaFile.Read(metaData, 0, metaData.Length);
                            File.WriteAllBytes($@"{lunarPath}\meta_008_0000.bin", metaData);
                        }
                    }
                }
            }
        }
    }
}
