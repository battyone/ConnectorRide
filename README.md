# Microsoft Connector Data

Microsoft Connector routes in simple JSON and [GTFS](https://developers.google.com/transit/gtfs/) format.

## Notice

**This project is not affiliated with Microsoft in any way.** The Microsoft Connector buses themselves are only intended for Microsoft employees. I don't run the Connector Ride website or make any of the rules. I just wanted to make data that is already publicly visible more convenient for developers. This project should not be considered the official source of Microsoft Connector data.

If this is a problem, I'll gladly take the project down. Just contact me using the email address on [my website](http://joelverhagen.com/).

## Goals

1. To make Microsoft transportation information available to developers, to encourage innovation and the use of public transportation.
1. To be a good internet citizen and not place abnormal load on the website being scraped.
1. To encourage the use of the GTFS format.

## Overview

The data is retrieved by scraping information from the Microsoft Connector routes website. The information is then make available in three formats:

1. A JSON document that contains is all of the data scraped from the [Connector website](http://connectorride.mobi). The intent of this document is be as close to the original form as possible while still being convenient to code against. 
1. A GTFS feed .zip archive (generated from #1, the JSON document) where AM and PM routes are grouped into a single route.
1. A GTFS feed .zip archive (also generated from #1) where AM and PM routes are not grouped.

Currently, all of the required GTFS files are included in the feed as well as one optional file (shapes.txt). The feed passes validation using [FeedValidator](https://github.com/google/transitfeed/wiki/FeedValidator).

## Example

This is what the GTFS data looks like in the `schedule_viewer` tool (available in the [FeedValidator](https://github.com/google/transitfeed/wiki/FeedValidator) release).

![Example GTFS](https://github.com/joelverhagen/ConnectorRide/blob/master/example-gtfs.png?raw=true)

## Data

The data is availabe in Azure Blob Storage. The link provided below will always point to the latest available data for each of the formats mentioned above:

Format           | Latest URL
---------------- | ----------------------------------------------------------------------------
JSON             | https://connectorride.blob.core.windows.net/scrape/schedules/latest.json
GTFS             | https://connectorride.blob.core.windows.net/scrape/gtfs/latest.zip
GTFS (ungrouped) | https://connectorride.blob.core.windows.net/scrape/gtfs-ungrouped/latest.zip
Last Processed   | https://connectorride.blob.core.windows.net/scrape/schedules/latest-status.json

## Packages

You can deserialize the JSON feed mentioned into the `ScrapeResult` model found in the following package:

```
Install-Package Knapcode.ConnectorRide.Common
```

## Future

1. Scrape [msshuttle.mobi](http://msshuttle.mobi).
1. Run a [OneBusAway server](https://github.com/OneBusAway/onebusaway/wiki/Running-Onebusaway).
1. Run an [OpenTripPlanner server](http://www.opentripplanner.org/). 
