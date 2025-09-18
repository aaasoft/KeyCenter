using System.Security.Cryptography;
using KeyCenter.Core;
using KeyCenter.Core.Utils;
using YiQiDong.Agent;
using YiQiDong.Core;

namespace KeyCenter.Blazor;

public class Agent : AbstractAgent
{
    public static Agent Instance { get; private set; }
    private CancellationTokenSource cts;
    private IHost host;
    public ConfigModel Config { get; private set; }
    public Server KeyCenterServer { get; private set; }

    public Agent()
    {
        Instance = this;
    }

    public override void Init()
    {
        base.Init();
        AddFunction(new Functions.Config());
    }

    public IHostBuilder CreateHostBuilder(string[] args) =>

    
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
#if !DEBUG
                webBuilder.UseContentRoot(AgentContext.Container.ImageFolder);
#endif
                webBuilder.ConfigureLogging(logging => logging.ClearProviders());
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls(Config.Urls.Split(new char[] { ',', ';' }));
            });

    public override void Start()
    {
        base.Start();
        Config = Functions.Config.Instance.ReadConfig();
        //生成RSA密钥对
        if (string.IsNullOrEmpty(Config.PublicKey) || string.IsNullOrEmpty(Config.PrivateKey))
        {
            var rSACryptoServiceProvider = RSA.Create(1024);
            Config.PublicKey = rSACryptoServiceProvider.ToXmlString(includePrivateParameters: false);
            Config.PrivateKey = rSACryptoServiceProvider.ToXmlString(includePrivateParameters: true);
            Functions.Config.Instance.WriteConfig(Config);
        }
        KeyCenterServer = new Server(Config.GetProducts(), Config.PublicKey, Config.PrivateKey);

        cts = new CancellationTokenSource();
        var token = cts.Token;
        host = CreateHostBuilder([]).Build();

        Task.Run(() =>
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;
                try
                {
                    host.StartAsync().Wait();
                    break;
                }
                catch (Exception ex)
                {
                    var message = $"Web服务启动失败，请检查端口是否被占用。错误详细：" + ex.Message;
                    Console.Error.WriteLine(message);
                    if (AgentContext.Container == null)
                        throw new Exception(message, ex);
                    Thread.Sleep(5000);
                }
            }
        });
    }

    public override void Stop()
    {
        cts?.Cancel();
        cts = null;

        try
        {
            host.Dispose();
        }
        catch { }
        base.Stop();
    }
}