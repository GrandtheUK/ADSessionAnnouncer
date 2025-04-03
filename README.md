# ADSessionAnnouncer

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) to announce your session to a restricted session system like the AD Session Browser. This is a headless only mod and will not work on the client.

If enabled, ADSessionAnnouncer hooks into when a world is created and starts a POST request every 30 seconds to the specified Server with the details of your session needed to be on the AD Session Browser. 

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [ADSessionAnnouncer.dll](https://github.com/GrandtheUK/ADSessionAnnouncer/releases/latest/download/ADSessionAnnouncer.dll) into your `rml_mods` folder on your headless. This folder should be at `{steam path}\steamapps\common\Resonite\Headless\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create this folder for you.
1. Start the Headless. If you want to verify that the mod is working you can check your Resonite logs.

## AS Session Browser Key
In order to publish to an AD Session Browser you have to have a key to push to the server the browser pulls from, without the key you cannot push. A key should be reasonably strong to avoid someone stealing it and making requests as though they were you. For more details see [the AD Session Browser documentation](https://github.com/Resonite-Community-Projects/adult-session-browser?tab=readme-ov-file) and specifically the section on [the Update System and key access](https://github.com/Resonite-Community-Projects/adult-session-browser?tab=readme-ov-file#accessing-hidden-sessions). 


## Configuration
ADSessionAnnouncer should auto-generate a template in `Resonite\Headless\rml_config\` on its first launch.

The mod has global options for the ServerKey to use to make the requests (which must be registered with the server browser host you use) and a server to make the requests to. also there are also options on how to display your community with the CommunityName (the name to show for your category), DiscordLink (a link to your community discord) and LogoUri (a link to an image of your community logo).

A Session is announced if it's SessionId is configured in the Announcer config under the `Sessions` list. Each session can declare its own preview image (in the form of a uri which can be either a resdb link or a normal weblink to an image). The following is a snippet for a world configuration to be placed into the `Sessions` list in the config.
```json
{
    "SessionId":"S-U-Yourheadless:SessionName",
    "PreviewUri":"resdb:///220353c878315cee79a36862d4773a88de80d67155ffde5e14d0abb9097a011b.webp"
}
```

## Planned
- Command to start new AD Session without editing the config before launch
- Look into if making this mod work with the client is possible
- config option and command for enabling or disabling announcing a session individually