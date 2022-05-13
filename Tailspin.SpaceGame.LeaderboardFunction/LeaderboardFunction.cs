using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TailSpin.SpaceGame.LeaderboardFunction;
using System.Collections.Generic;
using System.Linq;

namespace Tailspin.SpaceGame.LeaderboardFunction
{
    public class LeaderboardFunction
    {
        // High score repository.
        private readonly IDocumentDBRepository<Score> _scoreRepository;

        // User profile repository.
        private readonly IDocumentDBRepository<Profile> _profileRespository;

        public LeaderboardFunction(
            IDocumentDBRepository<Score> scoreRepository,
            IDocumentDBRepository<Profile> profileRespository)

        {
            this._scoreRepository = scoreRepository;
            this._profileRespository = profileRespository;
        }

        [FunctionName("LeaderboardFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Leaderboard function processed a request.");

            // Grab parameters from query string.
            string mode = req.Query["mode"];
            string region = req.Query["region"];

            int page = 0;
            int.TryParse(req.Query["page"], out page);

            int pageSize;
            if (int.TryParse(req.Query["pageSize"], out pageSize))
            {
                pageSize = Math.Max(Math.Min(pageSize, 50), 1);
            }
            else
            {
                pageSize = 10;
            }

            // Create the baseline response.
            var leaderboardResponse = new LeaderboardResponse()
            {
                Page = page,
                PageSize = pageSize,
                SelectedMode = mode,
                SelectedRegion = region
            };

            // Form the query predicate.
            // Select all scores that match the provided game mode and region (map).
            // Select the score if the game mode or region is empty.
            Func<Score, bool> queryPredicate = score =>
                (string.IsNullOrEmpty(mode) || score.GameMode == mode) &&
                (string.IsNullOrEmpty(region) || score.GameRegion == region);

            // Fetch the total number of results in the background.
            var countItemsTask = this._scoreRepository.CountItemsAsync(queryPredicate);

            // Fetch the scores that match the current filter.
            IEnumerable<Score> scores = await this._scoreRepository.GetItemsAsync(
                queryPredicate, // the predicate defined above
                score => score.HighScore, // sort descending by high score
                page - 1, // subtract 1 to make the query 0-based
                pageSize
              );

            // Wait for the total count.
            leaderboardResponse.TotalResults = await countItemsTask;

            // Fetch the user profile for each score.
            // This creates a list that's parallel with the scores collection.
            var profiles = new List<Task<Profile>>();
            foreach (var score in scores)
            {
                profiles.Add(this._profileRespository.GetItemAsync(score.ProfileId));
            }
            Task<Profile>.WaitAll(profiles.ToArray());

            // Combine each score with its profile.
            leaderboardResponse.Scores = scores.Zip(profiles, (score, profile) => new ScoreProfile { Score = score, Profile = profile.Result });

            return (ActionResult)new OkObjectResult(leaderboardResponse);
        }
    }
}
