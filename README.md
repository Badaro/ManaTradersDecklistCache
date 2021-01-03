# ManaTradersDecklistCache
This repository contains a cache in JSON format of tournaments from the ManaTraders series.

* Updater -> Tool to update the repository
* Validator -> Tool to check for errors in the repository
* Tournaments -> Tournament repository, organized by the date they were posted on the ManaTraders website

Each JSON file contains a tournament object, an array of decks, plus standings and bracket information when appropriate. Check the MTGODecklistParser (https://github.com/Badaro/MTGODecklistParser/tree/master/MTGODecklistParser/Model) repository to see exactly what these entities contain.
