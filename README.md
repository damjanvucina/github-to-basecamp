# Github to Basecamp
Integration of Github and Basecamp using Octokit and Basecamp 3 API in .NET. Built on top of Azure Topic, Queue and Blob storage services. Communication performance boosted utilizing Protocol Buffers. Apps' state kept in MongoDB.

## What the heck is Basecamp?
[Project collab at its finest (2 min video)](https://basecamp.com/how-it-works)


## How it works

- The idea is that each Github repo maps to a single project in Basecamp
- Generate OAuth tokens on each app
- Persist the tokens in addition to Github repo name and Basecamp project id to MongoDB
- Have your commits (alongside commit message, comments and various other commit and its author details) as well as all the files hit by each commit instantly transfered to Basecamp. 
- Each commit generates a new Campfire line and a new Message Board message with all of the above-mentioned details
- All the newly changed files within the Github repo are transfered to Basecamp's Docs&Files 
