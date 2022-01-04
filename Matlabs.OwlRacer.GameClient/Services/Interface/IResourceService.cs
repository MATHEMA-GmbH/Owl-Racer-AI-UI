using System.Collections.Generic;
using System.Threading.Tasks;
using Matlabs.OwlRacer.Protobuf;

namespace Matlabs.OwlRacer.GameClient.Services.Interface
{
    public interface IResourceService
    {
        Task<ResourceImagesDataResponse> GetBaseImageDataAsync();
        Task<TrackImageDataResponse> GetTrackImageDataAsync(int trackNumber);
        Task<TrackData> GetTrackDataAsync(int trackNumber);
        Task<Dictionary<int, byte[]>> GetStartPhaseImageData();
    }
}
