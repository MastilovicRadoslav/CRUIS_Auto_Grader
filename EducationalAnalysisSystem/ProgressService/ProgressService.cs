using Common.DTOs;
using Common.Interfaces;
using Common.Models;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgressService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class ProgressService : StatelessService, IProgressService
    {
        public ProgressService(StatelessServiceContext context)
            : base(context)
        { }

        public async Task<EvaluationStatisticsDto> AnalyzeProgressAsync(List<FeedbackDto> works)
        {
            var completedWorks = works
                .Where(w => w.Grade > 0)
                .OrderBy(w => w.EvaluatedAt)
                .ToList();

            var total = completedWorks.Count;
            var avg = total > 0 ? completedWorks.Average(w => w.Grade) : 0;

            var gradeDist = completedWorks
                .GroupBy(w => w.Grade)
                .ToDictionary(g => g.Key, g => g.Count());

            var timeline = completedWorks
                .Select(w => new Tuple<DateTime, int>(w.EvaluatedAt, w.Grade))
                .ToList();

            var issueCounter = new Dictionary<string, int>();
            foreach (var work in completedWorks)
            {
                foreach (var issue in work.IdentifiedErrors ?? new List<string>())
                {
                    if (issueCounter.ContainsKey(issue))
                        issueCounter[issue]++;
                    else
                        issueCounter[issue] = 1;
                }
            }

            var stats = new EvaluationStatisticsDto
            {
                TotalWorks = total,
                AverageGrade = Math.Round(avg, 2),
                GradeDistribution = gradeDist,
                GradeTimeline = timeline,
                MostCommonIssues = issueCounter,
                Above9 = completedWorks.Count(w => w.Grade >= 9),
                Between7And8 = completedWorks.Count(w => w.Grade >= 7 && w.Grade < 9),
                Below7 = completedWorks.Count(w => w.Grade < 7)
            };

            return await Task.FromResult(stats);
        }


        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        // Podešavanje slušalaca za obradu zahteva
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
