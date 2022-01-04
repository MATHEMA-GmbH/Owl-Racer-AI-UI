using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.Protobuf;
using Microsoft.Extensions.Logging;

namespace Matlabs.OwlRacer.GameClient.Services
{
    public class GameService : IGameService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly GrpcCoreService.GrpcCoreServiceClient _coreClient;

        public GameService(ILogger<SessionService> logger, GrpcCoreService.GrpcCoreServiceClient coreService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _coreClient = coreService ?? throw new ArgumentNullException(nameof(coreService));
        }

        public async Task<RaceCar> CreateRaceCarAsync(Session session, float maxVelocity, float acceleration, string name, string color)
        {
            if(session == null) { throw new ArgumentNullException(nameof(session)); }

            var response = await _coreClient.CreateCarAsync(new CreateCarData
            {
                SessionId = new GuidData
                {
                    GuidString = session.Id.ToString()
                },
                MaxVelocity = maxVelocity,
                Acceleration = acceleration,
                Name = name,
                Color = color,
            });

            return new RaceCar(Guid.Parse(response.SessionId.GuidString), maxVelocity, acceleration, name, color)
            {
                Id = Guid.Parse(response.Id.GuidString)
            };
        }

        public async Task DestroyRaceCarAsync(RaceCar car)
        {
            if(car == null) { throw new ArgumentNullException(nameof(car)); }

            await _coreClient.DestroyCarAsync(new GuidData
            {
                GuidString = car.Id.ToString()
            });
        }

        public async Task<RaceCarData> StepAsync(RaceCar car, StepData.Types.StepCommand stepCommand)
        {
            try
            {
                return await _coreClient.StepAsync(new StepData
                {
                    CarId = new GuidData
                    {
                        GuidString = car.Id.ToString()
                    },

                    Command = stepCommand
                });
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Unable to step race car with ID {car.Id}: {e.Message}");
                return null;
            }
        }

        public async Task UpdateRaceCarDataAsync(RaceCar raceCar)
        {
            if (raceCar == null) { throw new ArgumentNullException(nameof(raceCar)); }

            try
            {
                var data = await _coreClient.GetCarDataAsync(new GuidData {GuidString = raceCar.Id.ToString()});
                raceCar.Position = new Vector2(data.Position.X, data.Position.Y);
                raceCar.Rotation = data.Rotation;
                raceCar.Velocity = data.Velocity;
                raceCar.IsCrashed = data.IsCrashed;
                raceCar.UnCrashed = data.UnCrashed;
                raceCar.WrongDirection = data.WrongDirection;
                raceCar.ScoreStep = data.ScoreStep;
                raceCar.ScoreOverall = data.ScoreOverall;
                raceCar.Ticks = data.Ticks;
                raceCar.Distance.Front = data.Distance.Front;
                raceCar.Distance.FrontLeft = data.Distance.FrontLeft;
                raceCar.Distance.FrontRight = data.Distance.FrontRight;
                raceCar.Distance.Left = data.Distance.Left;
                raceCar.Distance.Right = data.Distance.Right;
                raceCar.Distance.MaxViewDistance = data.Distance.MaxViewDistance;
                raceCar.Checkpoint = data.CheckPoint;
                raceCar.NumRounds = data.NumRounds;
                raceCar.NumCrashes = data.NumCrashes;
                raceCar.Color = data.Color;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Data for race car with ID {raceCar.Id} could not be updated: {e.Message}");
            }
        }

        public async Task<IEnumerable<Guid>> GetRaceCarIdsAsync(Guid sessionId)
        {
            var result = await _coreClient.GetCarIdsAsync(new GuidData { GuidString = sessionId.ToString() });
            return result.Guids.Select(x => Guid.Parse(x.GuidString)).ToList();
        }

        public async Task ResetCarAsync(RaceCar raceCar)
        {
            await _coreClient.ResetAsync(new GuidData { GuidString = raceCar.Id.ToString() });
        }

        public async Task<RaceCarData> GetRaceCarDataAsync(RaceCar car)
        {
            return await _coreClient.GetCarDataAsync(new GuidData
            {
                GuidString = car.Id.ToString()
            });
        }
    }
}
