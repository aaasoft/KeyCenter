using System.Text.Json.Serialization;
using KeyCenter.Core.Models;

namespace KeyCenter.Blazor
{
    [JsonSerializable(typeof(ConfigModel))]
    internal partial class ConfigModelSerializerContext : JsonSerializerContext { }

    public class ConfigModel
    {
        public string Title { get; set; } = "授权中心";
        public string Urls { get; set; } = "http://*:3000";
        public string Password { get; set; } = "admin";        
        public string Products { get; set; } = @"TestProduct1=测试产品1
TestProduct2=测试产品2
TestProduct3=测试产品3";
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }

        public ProductInfo[] GetProducts()
        {
            var list = new List<ProductInfo>();
            foreach (var line in Products.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var strs = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (strs.Length < 2)
                    continue;
                var id = strs[0].Trim();
                var name = strs[1].Trim();
                list.Add(new ProductInfo() { Id = id, Name = name });
            }
            return list.ToArray();
        }
    }
}
