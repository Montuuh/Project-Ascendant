using System;

namespace ProjectAscendant.Core
{
    // Per §9.8.3 — prepended to every save file for version + integrity checks.
    [Serializable]
    public struct SaveHeader
    {
        public int    SchemaVersion;
        public string GameVersion;
        public string Timestamp;  // ISO-8601 UTC string; JsonUtility doesn't serialize DateTime.
        public uint   Checksum;   // FNV1a of the data JSON that follows the header.
    }
}
