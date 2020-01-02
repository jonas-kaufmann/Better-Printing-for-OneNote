using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Setup.GUIDs
{
    public class GUIDReaderWriter
    {
        public readonly string FilePath;

        public GUIDReaderWriter(string filePath)
        {
            FilePath = filePath;
        }

        public Guid GetGUIDForVersion(Version version)
        {
            JsonSerializer json = new JsonSerializer();
            GUIDModel[] guids = null;

            if (File.Exists(FilePath))
            {
                using (StreamReader file = File.OpenText(FilePath))
                {
                    guids = (GUIDModel[])json.Deserialize(file, typeof(GUIDModel[]));
                    GUIDModel guid = guids.FirstOrDefault(g => g.Version == version.ToString());

                    if (guid != null)
                        return guid.GUID;
                }
            }

            // if version does not exist in file, generate new GUID for it and write the updated file
            GUIDModel newGuid = new GUIDModel
            {
                Version = version.ToString(),
                GUID = Guid.NewGuid()
            };

            GUIDModel[] newGuids;
            if (guids == null)
                newGuids = new GUIDModel[1];
            else
            {
                newGuids = new GUIDModel[guids.Length + 1];
                guids.CopyTo(newGuids, 0);
            }
            newGuids[newGuids.Length - 1] = newGuid;

            using (StreamWriter writer = File.CreateText(FilePath))
            {
                json.Serialize(writer, newGuids, typeof(GUIDModel[]));
            }

            return newGuid.GUID;
        }
    }
}
