using System.Security.Cryptography;
using System.Text;
using KeyCenter.Core.Models;
using KeyCenter.Core.Utils;

namespace KeyCenter.Core;

public class Server
{
    private ProductInfo[] products;
    private RSAParameters publicKey;
    private RSAParameters privateKey;

    public Server(ProductInfo[] products, string publicKeyString, string privateKeyString)
    {
        this.products = products;
        publicKey = RsaUtils.DecodePublicKeyFromXml(publicKeyString);
        privateKey = RsaUtils.DecodePrivateKeyFromXml(privateKeyString);
    }

    public ProductInfo[] GetProducts() => products;

    public string GetProductName(string product)
    {
        return products.FirstOrDefault(t => t.Id == product)?.Name ?? product;
    }

    public string GenerateLicenseKey(string machineId, string product, DateTime startTime, DateTime endTime)
    {
        try
        {
            var sb = new StringBuilder();

            List<Byte> byteList = new List<byte>();
            //此16字节为ID
            byteList.AddRange(Guid.NewGuid().ToByteArray());
            //此16字节为机器码
            try
            {
                byteList.AddRange(BytesUtils.GetBytesFromByteString(machineId));
            }
            catch
            {
                return $"无效的机器码：[{machineId}]";
            }
            //此16字节为产品名称Hash值
            byteList.AddRange(Md5Utils.ComputeMD5Hash(new UTF8Encoding(false).GetBytes(product)));
            //此8字节(大端字节序)为生效时间的Ticks
            byteList.AddRange(BytesUtils.GetBytesWithBigEndian(startTime.Ticks));
            //此8字节(大端字节序)为过期时间的Ticks
            byteList.AddRange(BytesUtils.GetBytesWithBigEndian(endTime.Ticks));

            byte[] lecenseContextBytes = byteList.ToArray();

            byte[] signResult = RsaUtils.SignData(lecenseContextBytes, privateKey);
            byteList.AddRange(signResult);

            String tmpText = BitConverter.ToString(byteList.ToArray()).Replace("-", "");
            String tmpText2 = "";
            const Int32 lineMaxCount = 32;
            for (int i = 0; i < tmpText.Length / lineMaxCount; i++)
            {
                tmpText2 += tmpText.Substring(i * lineMaxCount, lineMaxCount) + Environment.NewLine;
            }
            tmpText2 = tmpText2.Trim();
            return $@"机器码:{machineId}
授权产品:{GetProductName(product)}
生效时间:{startTime.ToString("yyyy-MM-dd HH:mm:ss")}
到期时间:{endTime.ToString("yyyy-MM-dd HH:mm:ss")}
授权码:
----------------
{tmpText2}";
        }
        catch
        {
            return "ERROR!";
        }
    }


    public string Validate(String licenseKey)
    {
        var licenseIdBytes = new Byte[16];
        var machineIdBytes = new Byte[16];
        var productNameHashBytes = new Byte[16];
        var effectiveTimeBytes = new Byte[8];
        var expirationTimeBytes = new Byte[8];
        var licenseSign = new Byte[128];


        byte[] licenseData;
        try
        {
            licenseKey = licenseKey.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            licenseData = BytesUtils.GetBytesFromByteString(licenseKey);
        }
        catch
        {
            return "000:无效的授权码，无效16进制字符串！";
        }
        if (licenseData.Length != 192)
        {
            return "001:无效的授权码，长度不正确！";
        }

        MemoryStream stream = new MemoryStream(licenseData);
        Byte[] licenseContent = new Byte[64];
        //正文
        stream.Read(licenseContent, 0, licenseContent.Length);
        //签名
        stream.Read(licenseSign, 0, licenseSign.Length);
        stream.Close();
        //验证签名是否正确
        if (!RsaUtils.VerifyData(licenseContent, licenseSign, publicKey))
        {
            return "002:无效的授权码，签名验证失败！";
        }
        var sb = new StringBuilder();

        //读取正文内容
        stream = new MemoryStream(licenseContent);
        //ID
        stream.Read(licenseIdBytes, 0, licenseIdBytes.Length);
        var licenseId = BitConverter.ToString(licenseIdBytes).Replace("-", "");
        //机器码
        stream.Read(machineIdBytes, 0, machineIdBytes.Length);
        var machineId = BitConverter.ToString(machineIdBytes).Replace("-", "");
        sb.AppendLine("机器码:" + machineId);

        //产品名称Hash值
        stream.Read(productNameHashBytes, 0, productNameHashBytes.Length);
        var productNameHash = BitConverter.ToString(productNameHashBytes).Replace("-", "");

        var findProduct = false;
        foreach (var product in products)
        {
            if (Md5Utils.ComputeMD5Hash(product.Id) == productNameHash)
            {
                sb.AppendLine($"授权产品:{product.Name}");
                findProduct = true;
                break;
            }
        }
        if (!findProduct)
            sb.AppendLine($"警告:此授权码授权的产品未知！");
        //生效时间
        stream.Read(effectiveTimeBytes, 0, effectiveTimeBytes.Length);
        //过期时间
        stream.Read(expirationTimeBytes, 0, expirationTimeBytes.Length);

        //验证生效时间
        try
        {
            var effectiveTime = new DateTime(BytesUtils.GetInt64FromBytesWithBigEndian(effectiveTimeBytes));
            sb.AppendLine("生效时间:" + effectiveTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch
        {
            sb.AppendLine("005:无效的授权码，生效时间数据无效！");
            return sb.ToString();
        }
        //验证过期时间
        try
        {
            var expirationTime = new DateTime(BytesUtils.GetInt64FromBytesWithBigEndian(expirationTimeBytes));
            sb.AppendLine("过期时间:" + expirationTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch
        {
            sb.AppendLine("005:无效的授权码，过期时间数据无效！");
            return sb.ToString();
        }
        //返回结果
        return sb.ToString();
    }
}