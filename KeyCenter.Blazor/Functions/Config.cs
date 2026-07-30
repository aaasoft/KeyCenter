using Quick.Fields;
using YiQiDong.Protocol.V1.Model;
using YiQiDong.Core.Functions;
using YiQiDong.Agent;

namespace KeyCenter.Blazor.Functions;

public class Config : ModelJsonConfig<ConfigModel>
{
    public static Config Instance { get; private set; }

    public override string Name => "配置";

    public Config() : base(
        ConfigModelSerializerContext.Default.ConfigModel,
        AgentContext.Container?.ContainerFolder ?? AppContext.BaseDirectory,
        () => AgentContext.Container.AutoStart)
    {
        Instance = this;
    }

    private FieldForGet getWebGroup(FunctionRequest request, ConfigModel requestModel, bool isReadOnly = false)
    {
        var model = requestModel ?? Model;
        return new FieldForGet()
        {
            Id = "WebConfig",
            Type = FieldType.ContainerGroup,
            Name = "Web配置",
            Children =
            [
                new()
                {
                    Id =  nameof(ConfigModel.Title),
                    Name = "标题",
                    Description = "网页的标题",
                    Input_AllowBlank = false,
                    Type =  FieldType.InputText,
                    Value = model.Title,
                    Input_ReadOnly = isReadOnly
                },
                new()
                {
                    Id =  nameof(ConfigModel.Urls),
                    Name = "Web服务地址",
                    Description = null,
                    Input_AllowBlank = false,
                    Input_RegularExpression = "^http://((\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])|\\*)(\\:([0-9]|[1-9]\\d{1,3}|[1-5]\\d{4}|6[0-5]{2}[0-3][0-5]))?$",
                    Type =  FieldType.InputText,
                    Value = model.Urls,
                    Input_ReadOnly = isReadOnly
                },
                new()
                {
                    Id =  nameof(ConfigModel.Password),
                    Name = "密码",
                    Description = "授权的密码",
                    Input_AllowBlank = false,
                    Type =  FieldType.InputText,
                    Value = model.Password,
                    Input_ReadOnly = isReadOnly
                },
                new()
                {
                    Id =  nameof(ConfigModel.Products),
                    Name = "产品",
                    Input_AllowBlank = false,
                    Type =  FieldType.InputTextArea,
                    Value = model.Products,
                    Input_ReadOnly = isReadOnly
                },
                new()
                {
                    Id =  nameof(ConfigModel.PublicKey),
                    Name = "公钥",
                    Input_AllowBlank = false,
                    Type =  FieldType.InputTextArea,
                    Value = model.PublicKey,
                    Input_ReadOnly = isReadOnly
                },
                new()
                {
                    Id =  nameof(ConfigModel.PrivateKey),
                    Name = "私钥",
                    Input_AllowBlank = false,
                    Type =  FieldType.InputTextArea,
                    Value = model.PrivateKey,
                    Input_ReadOnly = isReadOnly
                }
            ]
        };
    }

    protected override List<FieldForGet> innerGet(FunctionRequest request, ConfigModel requestModel, bool isReadOnly = false)
    {
        return new List<FieldForGet>()
        {
            new FieldForGet()
            {
                Id="Tab",
                Type = FieldType.ContainerTab,
                Children =
                [
                    getWebGroup(request,requestModel,isReadOnly)
                ]
            }
        };
    }
}
