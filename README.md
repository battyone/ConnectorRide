# Microsoft Connector Data

Microsoft Connector routes in simple JSON and [GTFS](https://developers.google.com/transit/gtfs/) format.

## Notice

**This project is not affiliated with Microsoft in any way.** The Microsoft Connector buses themselves are intended for only Microsoft employees. I don't run the Connector Ride website or make any of the rules. I just wanted to the data that is already publicly visible to be more convenient for developers. This project should not be considered the official source of Microsoft Connector data.

If this is a problem, I'll gladly take the project down. Just contact me using the email address on [my website](http://joelverhagen.com/).

## Goals

1. To make Microsoft transportation information available to developers, to encourage innovation and the use of public transportation.
1. To be a good internet citizen and not place abnormal load on the website being scraped.
1. To encourage the use of the GTFS format.

## Overview

The data is retrieved by scraping information from the Microsoft Connector routes website. The information is then make available in three formats:

1. A JSON document that contains is all of the data scraped from the [Connector website](http://connectorride.mobi). The intent of this document is be as close to the original form as possible while still being convenient to code against. 
1. A GTFS feed .zip archive (generated from #1, the JSON document) where AM and PM routes are combined into a single route.
1. A GTFS feed .zip archive (also generated from #1) where AM and PM routes are not combined.

Currently, all of the required GTFS files are included in the feed as well as one optional file (shapes.txt). The feed passes validation using [FeedValidator](https://github.com/google/transitfeed/wiki/FeedValidator).

## Data

The data is available under the GitHub releases.

## Future

1. Identify the scraper HTTP requests with a relevant user agent.
1. Automatically update the data when it changes.
1. Scrape [msshuttle.mobi](http://msshuttle.mobi).
1. Run a [OneBusAway server](https://github.com/OneBusAway/onebusaway/wiki/Running-Onebusaway).
1. Run an [OpenTripPlanner server](http://www.opentripplanner.org/). 
