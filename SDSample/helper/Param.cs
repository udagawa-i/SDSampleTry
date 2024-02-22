using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SoundDesigner.Helper
{
    public class CPParamNameValue
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class CPMemory
    {
        public int id { get; set; }
        public List<CPParamNameValue> param { get; set; }
    }

    public class CPSystem
    {
        public List<CPParamNameValue> param { get; set; }
    }
    public class CPTransducer
    {
        public string id { get; set; }
        public string port { get; set; }
    }

    public class CPScratchData
    {
        public string csvalues { get; set; }

        public CPScratchData()
        {
            csvalues = ""; 
        }
    }

    public class CPVoiceAlert
    {
        public int alertindex { get; set; }
        public int hash { get; set; }
        public string wavefilename { get; set; }
        public byte[] encodeddata { get; set; }
    }

    public class Param
    {
        [JsonIgnore]
        public FileInfo Info { get; private set; }

        public string library { get; set; }
        public int libraryid { get; set; }
        public string product { get; set; }
        public string librarysignature { get; set; }
        public List<CPMemory> memory { get; set; }
        public CPSystem system { get; set; }
        public List<CPTransducer> transducer { get; set; }
        public CPScratchData scratchmemory { get; set; }
        public List<CPVoiceAlert> voicealerts { get; set; }

        public static Param OpenParamFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"The file specified is not present:" + path);
            }
            try
            {
                var sa = File.ReadAllText(path);
                var p = JsonConvert.DeserializeObject<Param>(sa);
                p.Info = new FileInfo(path);
                return p; 
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Deserialization error on the param file, the likely culprit is bad / unexpected format: " +
                    path);
            }
        }
  

    }
}