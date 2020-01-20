using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMWare.M2.Psb;

namespace ProjectLunarUI.M2engage
{
    public class SramEntry
    {
        public string tag = new string('\0', 8); //byte[8]
        public int offset = 0;
        public int size_enc = 0;
        public int size_dec = 0;
        public byte[] boundary = new byte[44];

        public static SramEntry FromByteArray(byte[] data)
        {
            SramEntry instance = new SramEntry();
            using (BinaryReader dataReader = new BinaryReader(new MemoryStream(data)))
            {
                instance.tag = Encoding.Default.GetString(dataReader.ReadBytes(8));
                instance.offset = dataReader.ReadInt32();
                instance.size_enc = dataReader.ReadInt32();
                instance.size_dec = dataReader.ReadInt32();
                instance.boundary = dataReader.ReadBytes(44);
            }
            return instance;
        }

        public byte[] ToByteArray()
        {
            byte[] data = new byte[64];

            Array.Copy(Encoding.Default.GetBytes(this.tag), 0, data, 0, 8);
            Array.Copy(BitConverter.GetBytes(this.offset), 0, data, 8, 4);
            Array.Copy(BitConverter.GetBytes(this.size_enc), 0, data, 12, 4);
            Array.Copy(BitConverter.GetBytes(this.size_dec), 0, data, 16, 4);
            Array.Copy(this.boundary, 0, data, 20, 44);

            return data;
        }
    }

    public class SramData
    {
        public SramEntry[] Sram_Entry = new SramEntry[4];
        public byte[] Sram_Image = new byte[8192];

        public SramData()
        {
            for (int i = 0; i < 4; i++)
            {
                this.Sram_Entry[i] = new SramEntry();
            }
        }

        public static SramData FromByteArray(byte[] data)
        {
            SramData instance = new SramData();
            using (BinaryReader dataReader = new BinaryReader(new MemoryStream(data)))
            {
                for (int i = 0; i < 4; i++)
                {
                    instance.Sram_Entry[i] = SramEntry.FromByteArray(dataReader.ReadBytes(64));
                }
                instance.Sram_Image = dataReader.ReadBytes(8192);
            }
            return instance;
        }

        public byte[] ToByteArray()
        {
            byte[] data = new byte[8448];
            for (int i = 0; i < 4; i++)
            {
                Array.Copy(Sram_Entry[i].ToByteArray(), 0, data, i * 64, 64);
            }
            Array.Copy(Sram_Image, 0, data, 256, 8192);

            return data;
        }
    }

    public class SettingGame
    {
        public byte[] data = new byte[128];
    }

    public class SystemData
    {
        public int SramCount
        {
            get; internal set;
        }

        public byte[] Base_Settings = new byte[0xEAC];
        public SettingGame[] Setting_Games;
        public int[] Work_Trial =  new int[256];
        public SramData[] Sram_Data;// = new SramData[Alldata.SRAM_Save_Count];

        public SystemData(int sramCount)
        {
            SramCount = sramCount;
            Setting_Games = new SettingGame[sramCount];
            Sram_Data = new SramData[SramCount];

            for (int i = 0; i < this.SramCount; i++)
            {
                this.Setting_Games[i] = new SettingGame();
                this.Sram_Data[i] = new SramData();
            }
        }

        public static SystemData FromByteArray(byte[] data, int sramCount)
        {
            SystemData instance = new SystemData(sramCount);
            using (BinaryReader dataReader = new BinaryReader(new MemoryStream(data)))
            {
                instance.Base_Settings = dataReader.ReadBytes(3756);
                for (int i = 0; i < instance.SramCount; i++)
                {
                    instance.Setting_Games[i] = new SettingGame() { data = dataReader.ReadBytes(128) };
                }
                for (int i = 0; i < 256; i++)
                {
                    instance.Work_Trial[i] = dataReader.ReadInt32();
                }
                for (int i = 0; i < instance.SramCount; i++)
                {
                    instance.Sram_Data[i] = SramData.FromByteArray(dataReader.ReadBytes(8448));
                }
            }
            return instance;
        }

        public byte[] ToByteArray()
        {
            int offset = 0;
            byte[] data = new byte[3756 + (SramCount * 128) + 1024 + (SramCount * 8448)];

            Array.Copy(Base_Settings, 0, data, offset, 3756);
            offset += 3756;

            for (int i = 0; i < SramCount; i++)
            {
                Array.Copy(Setting_Games[i].data, 0, data, offset, 128);
                offset += 128;
            }
            for (int i = 0; i < 256; i++)
            {
                Array.Copy(BitConverter.GetBytes(Work_Trial[i]), 0, data, offset, 4);
                offset += 4;
            }
            for (int i = 0; i < SramCount; i++)
            {
                Array.Copy(Sram_Data[i].ToByteArray(), 0, data, offset, 8448);
                offset += 8448;
            }

            return data;
        }
    }
}
