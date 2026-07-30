using System.Security.Cryptography;

namespace KeyCenter.Core.Utils
{
    public class RsaUtils
    {
        public static RSAParameters DecodePrivateKeyFromXml(String privateKeyXml)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.FromXmlString(privateKeyXml);
            return provider.ExportParameters(true);
        }

        public static RSAParameters DecodePublicKeyFromXml(String publicKeyXml)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.FromXmlString(publicKeyXml);
            return provider.ExportParameters(false);
        }

        public static byte[] Encrypt(byte[] data, RSAParameters key)
        {
            if (data.Length > 128)
            {
                throw new Exception("data的长度大于128字节！");
            }
            //自带的RSA加密，数据不能超过128字节，否则报错。
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            //导入Key
            provider.ImportParameters(key);
            return provider.Encrypt(data, false);
        }

        public static byte[] Decrypt(byte[] data, RSAParameters key)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            //导入Key
            provider.ImportParameters(key);
            return provider.Decrypt(data, false);
        }

        public static byte[] SignData(byte[] data, RSAParameters privateKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            //导入Key
            provider.ImportParameters(privateKey);
            return provider.SignData(data, SHA1.Create());
        }

        public static bool VerifyData(byte[] data, byte[] sign, RSAParameters publicKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            //导入Key
            provider.ImportParameters(publicKey);
            //找到对应的Hash类            
            return provider.VerifyData(data, SHA1.Create(), sign);
        }
    }
}
