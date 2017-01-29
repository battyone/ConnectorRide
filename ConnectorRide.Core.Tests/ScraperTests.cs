using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.ConnectorRide.Common.Models;
using Knapcode.ConnectorRide.Core.ClientModels;
using Knapcode.ToStorage.Core.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Knapcode.ConnectorRide.Core.Tests
{
    public class ScraperTests
    {
        [Fact]
        public async Task Scraper_ModelsMatch()
        {
            // Arrange
            var systemTime = new Mock<ISystemTime>();
            var utcNowA = new DateTimeOffset(2015, 1, 1, 8, 15, 30, TimeSpan.Zero);
            var utcNowB = utcNowA.AddMinutes(15);
            systemTime
                .SetupSequence(x => x.UtcNow)
                .Returns(utcNowA)
                .Returns(utcNowB)
                .Returns(utcNowA)
                .Returns(utcNowB);

            var scheduleReferenceA = BuildScheduleReference(1);
            var scheduleA = BuildSchedule(1);
            var mapA = BuildMap(1);
            
            var scheduleReferenceB = BuildScheduleReference(2);
            var scheduleB = BuildSchedule(2);
            var mapB = BuildMap(2);

            var client = new Mock<IClient>();
            client.Setup(x => x.GetScheduleReferencesAsync()).ReturnsAsync(new []{ scheduleReferenceA, scheduleReferenceB });

            client.Setup(x => x.GetScheduleAsync(scheduleReferenceA)).ReturnsAsync(scheduleA);
            client.Setup(x => x.GetMapAsync(scheduleA.MapReference)).ReturnsAsync(mapA);

            client.Setup(x => x.GetScheduleAsync(scheduleReferenceB)).ReturnsAsync(scheduleB);
            client.Setup(x => x.GetMapAsync(scheduleB.MapReference)).ReturnsAsync(mapB);

            var target = new Scraper(systemTime.Object, client.Object);
            var textWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(textWriter);
            var writer = new JsonScrapeResultWriter(jsonWriter);
            
            // Act
            var scrapeResult = await target.ScrapeAsync();
            await target.RealTimeScrapeAsync(writer);

            // Assert
            var deserialize = JsonConvert.DeserializeObject<ScrapeResult>(textWriter.ToString());
            deserialize.ShouldBeEquivalentTo(scrapeResult);
        }

        private ScheduleReference BuildScheduleReference(int id)
        {
            return new ScheduleReference {Href = BuildName(nameof(ScheduleReference), id) };
        }

        public ClientModels.Schedule BuildSchedule(int id)
        {
            return new ClientModels.Schedule
            {
                Name = BuildName(nameof(ClientModels.Schedule), id),
                Table = new Table
                {
                    Stops = new[]
                    {
                        new TableStop {Name = BuildName(nameof(TableStop), id)}
                    },
                    Trips = new[]
                    {
                        new TableTrip
                        {
                            StopTimes = new[]
                            {
                                new TableStopTime
                                {
                                    StopName = BuildName(nameof(TableStopTime), id)
                                }
                            }
                        },
                    }
                },
                MapReference = new MapReference
                {
                    Href = BuildName(nameof(MapReference), id)
                }
            };
        }

        private Map BuildMap(int id)
        {
            return new Map
            {
                Stops = new[]
                {
                    new MapStop {Name = BuildName(nameof(Map), id)},
                },
                Polyline = new Polyline
                {
                    MapWayPoints = new[]
                    {
                        new Location {Latitude = id}
                    }
                }
            };
        }

        private string BuildName(params object[] pieces)
        {
            return string.Join(string.Empty, pieces);
        }
    }
}
