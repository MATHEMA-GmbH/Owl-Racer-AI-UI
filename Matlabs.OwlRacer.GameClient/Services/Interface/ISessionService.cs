using System;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.Protobuf;

namespace Matlabs.OwlRacer.GameClient.Services.Interface
{
    public interface ISessionService
    {
        Session CreateSession(float gameTimeSetting, int trackNumber, string name);
        Session GetSession(Guid sessionId);
        void DestroySession(Session session);

        GuidListData GetSessionIds();
    }
}
