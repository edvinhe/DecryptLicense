using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace DecryptLicense
{
    class Program
    {
        static void Main(string[] args)
        {
          start2();
        }

      static void start2()
        {
          Console.WriteLine("========================Licence文件生成工具========================");
          Console.WriteLine();
          Console.Write("请输入machine文件路径：");
          String machineFilePath = Console.ReadLine();

          if (!String.IsNullOrWhiteSpace(machineFilePath))
          {
            FileUtil fileUtil = new FileUtil();
            List<byte> list = getBytes(machineFilePath);
            string input = Encoding.UTF8.GetString(fileUtil.decompressFile(list.ToArray()));
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            MachineInfo mf = serializer.Deserialize<MachineInfo>(input);
            // We get machine info now
            List<String> deviceList = getDeviceList(mf);

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"StationCode\":\"" + mf.StationCode + "\",\"DebugMode\":1,\"DeviceList\":[");
            foreach (string d in deviceList)
            {
              sb.Append("\"" + d + "\",");
            }

            sb.Remove(sb.ToString().Length - 1, 1);
            sb.Append("],\"Version\":\"1.0.0.2\",\"StartTimeValue\":\"2015-07-02 00:00:00\",\"EndTimeValue\":\"2065-07-02 00:00:00\"}");

            RSACryptoServiceProvider provider3 = new RSACryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(provider3.ToXmlString(true));
            byte[] src = fileUtil.compressFile(Encoding.UTF8.GetBytes(sb.ToString()));

            List<byte> result = new List<byte>();
            int srcOffset = 0;

            while (srcOffset < src.Length)
            {
              int count = ((src.Length - srcOffset) >= 100) ? 100 : (src.Length - srcOffset);
              byte[] dst = new byte[count];
              Buffer.BlockCopy(src, srcOffset, dst, 0, count);
              srcOffset += 100;
              byte[] collection = provider3.Encrypt(dst, false);
              result.AddRange(collection);
            }
            byte[] array = new byte[bytes.Length + result.Count];
            bytes.CopyTo(array, 0);
            result.CopyTo(array, bytes.Length);
            byte[] buffer6 = fileUtil.compressFile(array);

            string newLicenFilePath = machineFilePath.Substring(0,machineFilePath.LastIndexOf("\\")) + "\\licence";
            File.WriteAllBytes(newLicenFilePath, buffer6);
            Console.WriteLine("新的licence文件所在文件路径为：  " + newLicenFilePath);
          }
          else
          {
              Console.WriteLine("找不到machine文件，请检查文件路径是否正确");
          }

          Console.WriteLine();
          Console.WriteLine("按任意键退出......");
          Console.ReadLine();
        }

        static List<String> getDeviceList(MachineInfo mf)
        {
          List<int> deviceList = new List<int>();
          String tempList = mf.EquipmentsXml;

          Regex regex = new Regex(@"<IDENO_EQT>(.*?)</IDENO_EQT>");

          foreach (Match match in regex.Matches(tempList))
          {
            String d = match.Value.Substring(11, match.Value.IndexOf("</IDENO_EQT>")-11);
            if (!deviceList.Contains(Int32.Parse(d)))
            {
                 deviceList.Add(Int32.Parse(d));
            }
          }

          deviceList.Sort();

          List<String> result = new List<String>();
          foreach(int d in deviceList)
          {
            result.Add(d.ToString());
          }

          return result;
        }
        static List<byte> getBytes(string filePath)
        {
            FileUtil fileUtil = new FileUtil();
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.FromXmlString(fileUtil.getPublicKey(filePath));
            byte[] encryptData = fileUtil.getEncryptData(filePath);
            
            List<byte> list = new List<byte>();
            for (int i = 0; i < encryptData.Length; i += 0x80)
            {
                int count = ((encryptData.Length - i) >= 0x80) ? 0x80 : (encryptData.Length - i);
                byte[] dst = new byte[count];
                Buffer.BlockCopy(encryptData, i, dst, 0, count);
                list.AddRange(provider.Decrypt(dst, false));
            }

            return list;
        }
    }

    class FileUtil
    {
        public byte[] compressFile(byte[] rawData)
        {
            MemoryStream stream = new MemoryStream();
            GZipStream stream2 = new GZipStream(stream, CompressionMode.Compress, true);
            stream2.Write(rawData, 0, rawData.Length);
            stream2.Close();
            return stream.ToArray();
        }

        public string getPublicKey(String filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("无法找到密钥文件: " + filePath);
            }
            byte[] bytes = decompressFile(File.ReadAllBytes(filePath));
            Match match = Regex.Match(Encoding.UTF8.GetString(bytes), "<RSAKeyValue>.*</RSAKeyValue>");
            if (!match.Success)
            {
                throw new Exception("密钥格式不正确");
            }

            return match.Value;
        }
        public byte[] decompressFile(byte[] zippedData)
        {
            MemoryStream stream = new MemoryStream(zippedData);
            GZipStream stream2 = new GZipStream(stream, CompressionMode.Decompress);
            MemoryStream stream3 = new MemoryStream();
            byte[] buffer = new byte[0x400];
            while (true)
            {
                int count = stream2.Read(buffer, 0, buffer.Length);
                if (count <= 0)
                {
                    stream2.Close();
                    return stream3.ToArray();
                }
                stream3.Write(buffer, 0, count);
            }
        }

        public byte[] getEncryptData(String filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("无法找到文件: " + filePath);
            }
            byte[] bytes = decompressFile(File.ReadAllBytes(filePath));
            Match match = Regex.Match(Encoding.UTF8.GetString(bytes), "<RSAKeyValue>.*</RSAKeyValue>");
            if (!match.Success)
            {
                throw new Exception("machine文件不正确");
            }
            int length = Encoding.UTF8.GetBytes(match.Value).Length;
            byte[] dst = new byte[bytes.Length - length];
            Buffer.BlockCopy(bytes, length, dst, 0, dst.Length);

            return dst;
        }
    }
}
