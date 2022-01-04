using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.Protobuf;

namespace Matlabs.OwlRacer.GameClient.Services.Interface
{
    public interface IGameService
    {
        Task<RaceCar> CreateRaceCarAsync(Session session, float maxVelocity, float acceleration, string nam, string color);
        Task DestroyRaceCarAsync(RaceCar car);
        Task<RaceCarData> StepAsync(RaceCar car, StepData.Types.StepCommand stepCommand);
        Task UpdateRaceCarDataAsync(RaceCar raceCar);
        Task<IEnumerable<Guid>> GetRaceCarIdsAsync(Guid sessionId);
        Task ResetCarAsync(RaceCar raceCar);
        Task<RaceCarData> GetRaceCarDataAsync(RaceCar car);
    }
}
