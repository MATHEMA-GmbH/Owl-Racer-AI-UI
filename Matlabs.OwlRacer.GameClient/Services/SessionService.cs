using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.Common.Options;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.Protobuf;
using Microsoft.Extensions.Logging;

namespace Matlabs.OwlRacer.GameClient.Services
{
    public class SessionService : ISessionService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly GrpcCoreService.GrpcCoreServiceClient _coreClient;
        private readonly GrpcResourceService.GrpcResourceServiceClient _resourceClient;

        public SessionService(
            ILogger<SessionService> logger,
            GrpcCoreService.GrpcCoreServiceClient coreService,
            GrpcResourceService.GrpcResourceServiceClient resourceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _coreClient = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _resourceClient = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        }

        public Session CreateSession(float gameTimeSetting, int trackNumber, string name)
        {
            _logger.LogInformation($"Create Session");
            var sessionData = _coreClient.CreateSession(new CreateSessionData
            {
                GameTimeSetting = gameTimeSetting,
                Name = name,
                TrackNumber = trackNumber
            });

            var trackData = _resourceClient.GetTrackData(new TrackIdData
            {
                TrackNumber = trackNumber
            });

            return new Session
            {
                Id = Guid.Parse(sessionData.Id.GuidString),
                GameTimeSetting = sessionData.GameTimeSetting,
                Name = sessionData.Name,
                GameTime = sessionData.GameTime.ToDateTimeOffset().TimeOfDay * (sessionData.IsGameTimeNegative ? -1 : 1),
                IsPaused = sessionData.Phase == SessionData.Types.Phase.Pause,
                RaceTrack = new RaceTrack
                {
                    TrackNumber = trackData.TrackNumber,
                    StartPosition = new VectorOptions((int)trackData.StartPosition.X, (int)trackData.StartPosition.Y),
                    StartRotation = trackData.StartRotation,
                    StartLine = new StartLineOptions
                    {
                        Start = new VectorOptions((int)trackData.LinePositionStart.X, (int)trackData.LinePositionStart.Y),
                        End = new VectorOptions((int)trackData.LinePositionEnd.X, (int)trackData.LinePositionEnd.Y)
                    }
                },
                Scores = sessionData.Scores.ToDictionary(x => new RaceCar(
                    Guid.Parse(sessionData.Id.GuidString),
                    Guid.Parse(x.CarId.GuidString),
                    x.CarName,
                    ""),
                    x => x.Score)
            };
        }

        public Session GetSession(Guid sessionId)
        {
            var sessionData = _coreClient.GetSession(new GuidData { GuidString = sessionId.ToString() });

            var trackData = _resourceClient.GetTrackData(new TrackIdData
            {
                TrackNumber = sessionData.TrackNumber
            });

            return new Session
            {
                Id = Guid.Parse(sessionData.Id.GuidString),
                GameTimeSetting = sessionData.GameTimeSetting,
                Name = sessionData.Name,
                GameTime = sessionData.GameTime.ToDateTimeOffset().TimeOfDay * (sessionData.IsGameTimeNegative ? -1 : 1),
                IsPaused = sessionData.Phase == SessionData.Types.Phase.Pause,
                RaceTrack = new RaceTrack
                {
                    TrackNumber = trackData.TrackNumber,
                    StartPosition = new VectorOptions((int)trackData.StartPosition.X, (int)trackData.StartPosition.Y),
                    StartRotation = trackData.StartRotation,
                    StartLine = new StartLineOptions
                    {
                        Start = new VectorOptions((int)trackData.LinePositionStart.X, (int)trackData.LinePositionStart.Y),
                        End = new VectorOptions((int)trackData.LinePositionEnd.X, (int)trackData.LinePositionEnd.Y)
                    }
                },
                //Scores = sessionData.Scores.ToDictionary(x => new RaceCar(
                //    sessionId,
                //    Guid.Parse(x.CarId.GuidString),
                //    x.CarName,
                //   ""),
                //x => x.Score)
                Scores = UpdateScores(sessionData, sessionId)
            };
        }

        private Dictionary<RaceCar, int> UpdateScores(SessionData sessionData, Guid sessionId)
        {
            var Scores = sessionData.Scores.ToDictionary(x => new RaceCar(
            sessionId,
            Guid.Parse(x.CarId.GuidString),
            x.CarName,
            ""),
            x => x.Score);

            foreach (var car in Scores)
            {
                foreach (var score in sessionData.Scores)
                {
                    if (car.Key.Id.ToString() == score.CarId.GuidString)
                    {
                        car.Key.NumCrashes = score.NumCrashes;
                        car.Key.NumRounds = score.NumRounds;
                    }
                }
            }

            return Scores;
        }

        public GuidListData GetSessionIds()
        {
            var sessionIds = _coreClient.GetSessionIds(new Empty()); 
            return sessionIds;
        }

        public void DestroySession(Session session)
        {
            _logger.LogInformation($"Destroy Session");
            _coreClient.DestroySession(new GuidData
            {
                GuidString = session.Id.ToString()
            });
        }
    }
}
