# CampaignMod
What it does: It lets you create and load modded campaigns

Game Version: 1.24+

Mod Version: 1.0.0

Dependencies: 
- PolyTech Framework 0.7.5+
- Console Mod

To install: Place this .dll in the ...\Poly Bridge 2\BepInEx\plugins folder

# How to create and export campaigns
(You can open the console with [`])
Use "create_campaign <name>" to create an empty campaign
Load a layout in sandbox and use "add_this_level <campaign>" to add the level to the campaign (It wel set its owner to the setting "Name"), do this for every level you want to add. (You need to fill in a name in the workshop menu)
You can test the campaign using "load_campaign <campaign>"
To export it to a .campaign file use "export_campaign <campaign>"

# How to import and load campaigns
Put the .campaign file in BepInEx/plugins/CampaignMod/Exports (You can create the folder if it does not exist)
Use "import_campaign <name>" to import the campaign
Now use "load_campaign <campaign>" or "load_all_campaigns" to load the campaign

# Console Commands
- load_campaign <campaign>: loads a campaign

- load_all_campaigns: loads all the campaigns

- create_campaign <name>: creates an empty campaign

- remove_campaign <campaign>: removes a campaign

- campaign_info [campaign]: shows some info of a campaign or a list of all campaigns

- edit_campaign_info <campaign> <title|description|winmessage> <value>

- remove_level <campaign> <level>: removes a level from a campaign

- add_this_level <campaign> [position]: adds the currently loaded level in sandbox mode to a campaign, with setting "Name" as its owner

- unload_campaign <campaign>: unloads a campaign

- export_campaign <campaign>: exports a campaign to a .campaign file

- import_campaign <campaign>: imports a campaign from a .campaign file

# Settings
- Enable/Disable Mod: Enables/Disables the mod

- Name: Your name - gets used when you add a level to a campaign
