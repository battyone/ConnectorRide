using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.ClientModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
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

            var scheduleReferenceA = BuildScheduleReference("A");
            var scheduleA = BuildSchedule("A");
            var mapA = BuildMap("A");
            
            var scheduleReferenceB = BuildScheduleReference("B");
            var scheduleB = BuildSchedule("B");
            var mapB = BuildMap("B");

            var client = new Mock<IClient>();
            client.Setup(x => x.GetScheduleReferencesAsync()).ReturnsAsync(new []{ scheduleReferenceA, scheduleReferenceB });

            client.Setup(x => x.GetScheduleAsync(scheduleReferenceA)).ReturnsAsync(scheduleA);
            client.Setup(x => x.GetMapAsync(scheduleA.MapReference)).ReturnsAsync(mapA);

            client.Setup(x => x.GetScheduleAsync(scheduleReferenceB)).ReturnsAsync(scheduleB);
            client.Setup(x => x.GetMapAsync(scheduleB.MapReference)).ReturnsAsync(mapB);

            var target = new Scraper(systemTime.Object, client.Object);
            var stringWriter = new StringWriter();
            
            // Act
            var scrapeResult = await target.ScrapeAsync();
            await target.RealTimeScrapeAsync(stringWriter);

            // Assert
            var deserialize = JsonConvert.DeserializeObject<ScrapeResult>(stringWriter.ToString());
            deserialize.ShouldBeEquivalentTo(scrapeResult);
        }

        private ScheduleReference BuildScheduleReference(string name)
        {
            return new ScheduleReference {Href = BuildName(nameof(ScheduleReference), name)};
        }

        public ClientModels.Schedule BuildSchedule(string name)
        {
            return new ClientModels.Schedule
            {
                Name = BuildName(nameof(ClientModels.Schedule), name),
                Table = new Table
                {
                    Stops = new[]
                    {
                        new TableStop {Name = BuildName(nameof(TableStop), name)}
                    },
                    Trips = new[]
                    {
                        new TableTrip
                        {
                            StopTimes = new[]
                            {
                                new TableStopTime
                                {
                                    StopName = BuildName(nameof(TableStopTime), name)
                                }
                            }
                        },
                    }
                },
                MapReference = new MapReference
                {
                    Href = BuildName(nameof(MapReference), name)
                }
            };
        }

        private Map BuildMap(string name)
        {
            return new Map
            {
                Stops = new[]
                {
                    new MapStop {Name = BuildName(nameof(Map), name)}
                }
            };
        }

        private string BuildName(params string[] pieces)
        {
            return string.Join(string.Empty, pieces);
        }
    }
}
