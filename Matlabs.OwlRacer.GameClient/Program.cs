using System;
using System.IO;
using System.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using Matlabs.OwlRacer.Common.Options;
using Matlabs.OwlRacer.GameClient.Services;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.GameClient.States;
using Matlabs.OwlRacer.GameClient.States.Options;
using Matlabs.OwlRacer.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResourceService = Matlabs.OwlRacer.GameClient.Services.ResourceService;

namespace Matlabs.OwlRacer.GameClient
{
    public class Program
    {
        internal static ServiceProvider ServiceProvider { get; private set; }
        internal static IConfiguration Configuration { get; private set; }

        [STAThread]
        internal static void Main()
        {
            // Configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json");

            Configuration = configBuilder.Build();
            var agentOptions = new AgentOptions();
            var agentOptionsSection = Configuration.GetSection("Agent");
            agentOptionsSection.Bind(agentOptions);

            var pythonOptions = new PythonOptions();
            var pythonOptionsSection = Configuration.GetSection("Python");
            pythonOptionsSection.Bind(pythonOptions);

            var mlNetOptions = new MLNetOptions();
            var mlNetOptionsSection = Configuration.GetSection("MLNet");
            mlNetOptionsSection.Bind(mlNetOptions);

            var genericOptions = new GenericOptions();
            var genericOptionsSection = Configuration.GetSection("Generic");
            genericOptionsSection.Bind(genericOptions);

            var channel = new Channel($"{agentOptions.Address}:{agentOptions.Port}", ChannelCredentials.Insecure);
            //var channel = GrpcChannel.ForAddress($"{agentOptions.Address}:{agentOptions.Port}");
            
            var services = new ServiceCollection();

            services.AddSingleton(sp => channel);
            services.AddSingleton(sp => new GrpcCoreService.GrpcCoreServiceClient(channel));
            services.AddSingleton(sp => new GrpcResourceService.GrpcResourceServiceClient(channel));

            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IGameService, GameService>();
            services.AddTransient<IGameState<GameStateOptions>, GameState>(); //AddTransient
            services.AddTransient<IMenuState<MenuStateOptions>, MenuState>();
            services.AddTransient<IRankingState<RankingStateOptions>, RankingState>();
            services.AddScoped<OwlRacerGame>();
            services.AddScoped<IStateFactory, StateFactory>();

            services.Configure<AgentOptions>(agentOptionsSection);
            services.Configure<PythonOptions>(pythonOptionsSection);
            services.Configure<MLNetOptions>(mlNetOptionsSection);
            services.Configure<GenericOptions>(genericOptionsSection);

            services.AddLogging(conf =>
            {
                conf.AddConsole();
            });

            ServiceProvider = services.BuildServiceProvider();

            var logger = ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Starting OwlRacer Client for Agent at {agentOptions.Address}:{agentOptions.Port}");

            using var game = ServiceProvider.GetRequiredService<OwlRacerGame>();
            game.Run();


        }
    }
}
