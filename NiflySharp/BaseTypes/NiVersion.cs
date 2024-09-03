using System;

namespace NiflySharp
{
    public enum NiFileVersion : uint
    {
        V2_3 = 0x02030000,
        V3_0 = 0x03000000,
        V3_03 = 0x03000300,
        V3_1 = 0x03010000,
        V3_3_0_13 = 0x0303000D,
        V4_0_0_0 = 0x04000000,
        V4_0_0_2 = 0x04000002,
        V4_1_0_12 = 0x0401000C,
        V4_2_0_2 = 0x04020002,
        V4_2_1_0 = 0x04020100,
        V4_2_2_0 = 0x04020200,
        V5_0_0_1 = 0x05000001,
        V10_0_0_0 = 0x0A000000,
        V10_0_1_0 = 0x0A000100,
        V10_0_1_2 = 0x0A000102,
        V10_0_1_3 = 0x0A000103,
        V10_1_0_0 = 0x0A010000,
        V10_1_0_101 = 0x0A010065,
        V10_1_0_104 = 0x0A010068,
        V10_1_0_106 = 0x0A01006A,
        V10_1_0_114 = 0x0A010072,
        V10_2_0_0 = 0x0A020000,
        V10_2_0_1 = 0x0A020001,
        V10_3_0_1 = 0x0A030001,
        V10_4_0_1 = 0x0A040001,
        V20_0_0_4 = 0x14000004,
        V20_0_0_5 = 0x14000005,
        V20_1_0_1 = 0x14010001,
        V20_1_0_3 = 0x14010003,
        V20_2_0_5 = 0x14020005,
        V20_2_0_7 = 0x14020007,
        V20_2_0_8 = 0x14020008,
        V20_3_0_1 = 0x14030001,
        V20_3_0_2 = 0x14030002,
        V20_3_0_3 = 0x14030003,
        V20_3_0_6 = 0x14030006,
        V20_3_0_9 = 0x14030009,
        V20_5_0_0 = 0x14050000,
        V20_6_0_0 = 0x14060000,
        V20_6_5_0 = 0x14060500,
        V30_0_0_2 = 0x1E000002,
        V30_1_0_3 = 0x1E010003,
        Unknown = 0xFFFFFFFF
    }

    public class NiVersion
    {
        public const string NIF_GAMEBRYO = "Gamebryo File Format";
        public const string NIF_NETIMMERSE = "NetImmerse File Format";
        public const string NIF_NDS = "NDSNIF....@....@....";

        public NiFileVersion FileVersion { get; set; } = NiFileVersion.Unknown;

        public uint UserVersion { get; set; } = 0;

        public uint StreamVersion { get; set; } = 0;

        public uint NDSVersion { get; set; } = 0;

        public NiVersion() { }

        public NiVersion(NiFileVersion file, uint user, uint stream)
        {
            FileVersion = file;
            UserVersion = user;
            StreamVersion = stream;
        }

        // Construct a file version enumeration from individual values
        public static NiFileVersion ToFile(byte major, byte minor, byte patch, byte intern)
        {
            var ver = (major << 24) | (minor << 16) | (patch << 8) | intern;
            return (NiFileVersion)ver;
        }

        // Construct a file version enumeration from a string
        public static NiFileVersion ToFile(string verString)
        {
            if (string.IsNullOrWhiteSpace(verString))
                return NiFileVersion.Unknown;

            var verSplit = verString.Split('.', 4);
            if (verSplit.Length < 4)
                return NiFileVersion.Unknown;

            byte major = Convert.ToByte(verSplit[0]);
            byte minor = Convert.ToByte(verSplit[1]);
            byte patch = Convert.ToByte(verSplit[2]);
            byte intern = Convert.ToByte(verSplit[3]);

            return ToFile(major, minor, patch, intern);
        }

        // Return file version as individual values
        public static byte[] ToArray(NiFileVersion file)
        {
            return new byte[] { (byte)((uint)file >> 24), (byte)((uint)file >> 16), (byte)((uint)file >> 8), (byte)file };
        }

        public string VersionString
        {
            get
            {
                string verNum;
                var verArr = ToArray(FileVersion);

                if (FileVersion > NiFileVersion.V3_1)
                    verNum = $"{verArr[0]}.{verArr[1]}.{verArr[2]}.{verArr[3]}";
                else
                    verNum = $"{verArr[0]}.{verArr[1]}";

                string vstr;

                if (NDSVersion != 0)
                    vstr = NIF_NDS;
                else if (FileVersion < NiFileVersion.V10_0_0_0)
                    vstr = NIF_NETIMMERSE;
                else
                    vstr = NIF_GAMEBRYO;

                vstr += ", Version " + verNum;
                return vstr;
            }
        }

        public string GetVersionInfo()
        {
            return
                $"{VersionString}{Environment.NewLine}" +
                $"User Version: {UserVersion}{Environment.NewLine}" +
                $"Stream Version: {StreamVersion}";
        }

        public bool IsBethesda()
        {
            return (FileVersion == NiFileVersion.V20_2_0_7 && UserVersion >= 11) || IsOB();
        }

        public bool IsOB()
        {
            return ((FileVersion == NiFileVersion.V10_1_0_106 || FileVersion == NiFileVersion.V10_2_0_0) && UserVersion >= 3 && UserVersion < 11)
                || (FileVersion == NiFileVersion.V20_0_0_4 && (UserVersion == 10 || UserVersion == 11))
                || (FileVersion == NiFileVersion.V20_0_0_5 && UserVersion == 11);
        }

        public bool IsFO3()
        {
            return FileVersion == NiFileVersion.V20_2_0_7 && StreamVersion > 11 && StreamVersion < 83;
        }

        public bool IsSK()
        {
            return FileVersion == NiFileVersion.V20_2_0_7 && StreamVersion == 83;
        }

        public bool IsSSE()
        {
            return FileVersion == NiFileVersion.V20_2_0_7 && StreamVersion == 100;
        }

        public bool IsFO4()
        {
            return FileVersion == NiFileVersion.V20_2_0_7 && StreamVersion == 130;
        }

        public bool IsFO76()
        {
            return FileVersion == NiFileVersion.V20_2_0_7 && StreamVersion == 155;
        }

        public bool IsSF()
        {
            return FileVersion == NiFileVersion.V20_2_0_7 && StreamVersion == 172;
        }

        public static NiVersion GetOB()
        {
            return new NiVersion(NiFileVersion.V20_0_0_5, 11, 11);
        }

        public static NiVersion GetFO3()
        {
            return new NiVersion(NiFileVersion.V20_2_0_7, 11, 34);
        }

        public static NiVersion GetSK()
        {
            return new NiVersion(NiFileVersion.V20_2_0_7, 12, 83);
        }

        public static NiVersion GetSSE()
        {
            return new NiVersion(NiFileVersion.V20_2_0_7, 12, 100);
        }

        public static NiVersion GetFO4()
        {
            return new NiVersion(NiFileVersion.V20_2_0_7, 12, 130);
        }

        public static NiVersion GetFO76()
        {
            return new NiVersion(NiFileVersion.V20_2_0_7, 12, 155);
        }

        public static NiVersion GetSF()
        {
            return new NiVersion(NiFileVersion.V20_2_0_7, 12, 172);
        }
    }
}
