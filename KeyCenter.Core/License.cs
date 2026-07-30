using KeyCenter.Core.Utils;

namespace KeyCenter.Core
{
    public class License
    {
        private byte[] licenseIdBytes = new byte[16];
        private byte[] machineIdBytes = new byte[16];
        private byte[] productNameHashBytes = new byte[16];
        private byte[] effectiveTimeBytes = new byte[8];
        private byte[] expirationTimeBytes = new byte[8];
        private byte[] licenseSign = new byte[128];

        public string ProductNameHash { get; private set; }
        public string LicenseId { get; private set; }
        public string MachineId { get; private set; }
        public DateTime EffectiveTime { get; private set; }
        public DateTime ExpirationTime { get; private set; }

        //构造函数
        public License(string productName, string licenseKey, string publicKeyString)
        {
            var publicKey = RsaUtils.DecodePublicKeyFromXml(publicKeyString);
            byte[] licenseData;
            try
            {
                licenseKey = licenseKey.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                licenseData = BytesUtils.GetBytesFromByteString(licenseKey);
            }
            catch
            {
                throw new Exception("000:无效的授权码！");
            }
            if (licenseData.Length != 192)
            {
                throw new Exception("001:无效的授权码！");
            }

            MemoryStream stream = new MemoryStream(licenseData);
            byte[] licenseContent = new byte[64];
            //正文
            stream.Read(licenseContent, 0, licenseContent.Length);
            //签名
            stream.Read(licenseSign, 0, licenseSign.Length);
            stream.Close();
            //验证签名是否正确
            if (!RsaUtils.VerifyData(licenseContent, licenseSign, publicKey))
            {
                throw new Exception("002:无效的授权码！");
            }

            //读取正文内容
            stream = new MemoryStream(licenseContent);
            //ID
            stream.Read(licenseIdBytes, 0, licenseIdBytes.Length);
            LicenseId = BitConverter.ToString(licenseIdBytes).Replace("-", "");
            //机器码
            stream.Read(machineIdBytes, 0, machineIdBytes.Length);
            MachineId = BitConverter.ToString(machineIdBytes).Replace("-", "");

            //当机机器的机器码
            string currentMachineId = LicenseUtils.GetMachineId();
            if (MachineId != currentMachineId)
            {
                //授权码与当前机器码不匹配！
                throw new Exception("003:无效的授权码！");
            }

            //产品名称Hash值
            stream.Read(productNameHashBytes, 0, productNameHashBytes.Length);
            ProductNameHash = BitConverter.ToString(productNameHashBytes).Replace("-", "");

            string currentProductName = Md5Utils.ComputeMD5Hash(productName);
            if (ProductNameHash != currentProductName)
            {
                //授权产品与当前产品不匹配！
                throw new Exception("004:无效的授权码！");
            }
            //生效时间
            stream.Read(effectiveTimeBytes, 0, effectiveTimeBytes.Length);
            //过期时间
            stream.Read(expirationTimeBytes, 0, expirationTimeBytes.Length);

            //验证生效时间
            try
            {
                EffectiveTime = new DateTime(BytesUtils.GetInt64FromBytesWithBigEndian(effectiveTimeBytes));
                //如果当前时间小于生效时间
                if (DateTime.Now < EffectiveTime)
                {
                    throw new Exception("当前时间小于生效时间");
                }
            }
            catch
            {
                throw new Exception("005:无效的授权码！");
            }

            //验证过期时间
            try
            {
                ExpirationTime = new DateTime(BytesUtils.GetInt64FromBytesWithBigEndian(expirationTimeBytes));
                //如果当前时间大于过期时间
                if (DateTime.Now > ExpirationTime)
                {
                    throw new Exception("当前时间大于过期时间，此授权码已过期");
                }
            }
            catch
            {
                throw new Exception("006:无效的授权码！");
            }
        }
    }
}
