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
          generateLicenseFile();
        }

        static void start3()
        {
          FileUtil fu = new FileUtil();

          List<byte> originalLicense = getBytes(@"C:\Users\edhe.FAREAST\Documents\My Received Files\license");
          String originalStr = Encoding.UTF8.GetString(fu.decompressFile(originalLicense.ToArray()));
          Console.WriteLine(originalStr);


          Console.ReadLine();
        }

      static void generateLicenseFile()
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
            Console.WriteLine("生成简单license文件？ (yes/no)");
            string answer = Console.ReadLine();
            bool flag = true;
            if(answer.Trim().Equals("yes") || answer.Trim().Equals("Yes") || answer.Trim().Equals("YES"))
            {
              flag = true;
            }
            else
            {
              flag = false;
            }

            String deviceList = getDeviceList(mf, flag);

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"LocationStation\":{\"StationCode\":\""+ mf.StationCode+"\",\"StationName\":\""+mf.StationName+"\"},\"DebugMode\":1,\"DeviceList\":{");
            sb.Append(deviceList);
            sb.Append("},\"Version\":\"1.0.0.2\",\"StartTimeValue\":\"2016-10-13 00:00:00\",\"EndTimeValue\":\"2066-01-13 23:59:59\"}");

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

            string newLicenFilePath = machineFilePath.Substring(0,machineFilePath.LastIndexOf("\\")) + "\\license";
            File.WriteAllBytes(newLicenFilePath, buffer6);
            Console.WriteLine("新的license文件所在文件路径为：  " + newLicenFilePath);
          }
          else
          {
              Console.WriteLine("找不到machine文件，请检查文件路径是否正确");
          }

          Console.WriteLine();
          Console.WriteLine("按任意键退出......");
          Console.ReadLine();
        }

        static String getDeviceList(MachineInfo mf, bool simpleLicenseFile)
        {
          StringBuilder sb = new StringBuilder();
          String tempList = mf.EquipmentsXml;
          tempList = tempList.Substring(1);
          XmlDocument doc = new XmlDocument();
          doc.LoadXml(tempList);
          Dictionary<string, List<string>> deviceModelMap = new Dictionary<string, List<string>>();

          XmlNodeList xmlNodes = doc.DocumentElement.SelectNodes("/NewDataSet/Table");
          if (!simpleLicenseFile)
          {
            foreach (XmlNode xmlNode in xmlNodes)
            {
              List<string> ll = null;

              String deviceCode = xmlNode["NUM_EQT"].InnerText;
              String deviceID = xmlNode["IDENO_EQT"].InnerText;
              String deviceName = xmlNode["NAME_EQT"].InnerText;
              String deviceModel = xmlNode["MODNUM_EQT"].InnerText;
              String deviceIP = xmlNode["IPAdd_EQT"].InnerText;

              string temp = "{\"DeviceCode\":\"" + deviceCode + "\",\"DeviceId\":\"" + deviceID
                 + "\",\"DeviceName\":\"" + deviceName + "\",\"DeviceModel\":\"" + deviceModel
                 + "\",\"DeviceIp\":\"" + deviceIP + "\"},";

              deviceModelMap.TryGetValue(deviceModel, out ll);
              if (ll == null)
              {
                ll = new List<string>();
              }
              ll.Add(temp);

              if (!deviceModelMap.Keys.Contains(deviceModel))
              {
                deviceModelMap.Add(deviceModel, ll);
              }
            }

            foreach (String key in deviceModelMap.Keys)
            {
              List<String> dd = null;

              deviceModelMap.TryGetValue(key, out dd);
              dd[dd.Count - 1] = dd[dd.Count - 1].Substring(0, dd[dd.Count - 1].Length - 1);
            }

            foreach (string key in deviceModelMap.Keys)
            {
              sb.Append("\"" + key + "\":[");
              List<string> tt = null;

              deviceModelMap.TryGetValue(key, out tt);
              if (tt != null)
              {
                foreach (string kk in tt)
                {
                  sb.Append(kk);
                }
              }
              sb.Append("],");
            }

            String result = sb.ToString();

            return result.Substring(0, result.Length - 1);
          }
          else
          {
            sb.Append("\"PIS\":[");
            foreach (XmlNode xmlNode in xmlNodes)
            {
              if (xmlNode["MODNUM_EQT"].InnerText.Equals("PIS"))
              {
                String deviceCode = xmlNode["NUM_EQT"].InnerText;
                String deviceID = xmlNode["IDENO_EQT"].InnerText;
                String deviceName = xmlNode["NAME_EQT"].InnerText;
                String deviceModel = xmlNode["MODNUM_EQT"].InnerText;
                String deviceIP = xmlNode["IPAdd_EQT"].InnerText;

                string temp = "{\"DeviceCode\":\"" + deviceCode + "\",\"DeviceId\":\"" + deviceID
                   + "\",\"DeviceName\":\"" + deviceName + "\",\"DeviceModel\":\"" + deviceModel
                   + "\",\"DeviceIp\":\"" + deviceIP + "\"},";
                sb.Append(temp);
              }
            }

            String result = sb.ToString();
            result = result.Substring(0, result.Length - 1);
            result = result + "]";

            return result;
          }
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
