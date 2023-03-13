using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Google.Protobuf.WellKnownTypes;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.Protobuf;
using Microsoft.Extensions.Logging;

namespace Matlabs.OwlRacer.GameClient.Services
{
    public class ResourceService : IResourceService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly GrpcResourceService.GrpcResourceServiceClient _resourceClient;

        public ResourceService(ILogger<SessionService> logger, GrpcResourceService.GrpcResourceServiceClient resourceClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceClient = resourceClient ?? throw new ArgumentNullException(nameof(resourceClient));
        }

        public async Task<ResourceImagesDataResponse> GetBaseImageDataAsync()
        {
            return await _resourceClient.GetBaseImagesAsync(new Empty());
        }

        public async Task<TrackImageDataResponse> GetTrackImageDataAsync(int trackNumber)
        {
            return await _resourceClient.GetTrackImageAsync(new TrackIdData
            {
                TrackNumber = trackNumber
            });
        }

        public async Task<TrackData> GetTrackDataAsync(int trackNumber)
        {
            return await _resourceClient.GetTrackDataAsync(new TrackIdData
            {
                TrackNumber = trackNumber
            });
        }

        public async Task<Dictionary<int, byte[]>> GetStartPhaseImageData()
        {
            return new()
            {
                { 0, await File.ReadAllBytesAsync("Resources/light_0.png") },
                { 1, await File.ReadAllBytesAsync("Resources/light_1.png") },
                { 2, await File.ReadAllBytesAsync("Resources/light_1.png") },
                { 3, await File.ReadAllBytesAsync("Resources/light_2.png") },
                { 4, await File.ReadAllBytesAsync("Resources/light_2.png") },
                { 5, await File.ReadAllBytesAsync("Resources/light_3.png") },
            };
        }
    }
}
