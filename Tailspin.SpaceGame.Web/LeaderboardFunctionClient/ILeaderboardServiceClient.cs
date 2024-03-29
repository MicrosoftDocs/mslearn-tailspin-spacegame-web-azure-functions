﻿using System.Threading.Tasks;

namespace TailSpin.SpaceGame.Web
{
    public interface ILeaderboardServiceClient
    {
        Task<LeaderboardResponse> GetLeaderboard(int page, int pageSize, string mode, string region);
    }
}
