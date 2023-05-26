# Blackmagic Atem Audio Monitor / Switcher

This utility lets the Blackmagic ATEM mini automatically switch HDMI inputs based on an audio input of your choosing - particularly useful if you use it for live podcasting, where you have multiple people mic'd up with seperate video cameras and you want the cameras to follow the microphones, like in Zoom or Teams. 

Watch it in action here: 
https://www.youtube.com/watch?v=5Lme_szgvQk

> Note: C# is not my native environment, so please send pull requests / issues if you know of better ways to do things!

## THIS IS A BETA

This is the very first public release of this code, so there may be bugs and missing features.

# Building

For ease of use, included in this repo is the BMDSwitcherAPI.tlb file, built from 8.6.1 SDK BMDSwitcherAPI.idl file in https://www.blackmagicdesign.com/developer/product/atem . You should be able to just build from the Visual Studio IDE, no cmake required.

## Pre-built binaries

If you just want a pre-built x64 windows binary, grab the zip file from the [releases section](https://github.com/kris-sum/AtemSwitcher/releases) on github.
Note: Make sure you have the ATEM switchers software installed on your computer first from https://www.blackmagicdesign.com/support/family/atem-live-production-switchers .

## Example commandlines

List device inputs

    AtemAudioMonitorSwitcher.exe 192.168.250.81

Monitor audio inputs with a text-mode VU meter

    AtemAudioMonitorSwitcher.exe monitor 192.168.250.81

Auto-switch mic1 L to HDMI 1, mic1 R to HDMI 2, and mic3 to HDMI 3

    AtemAudioMonitorSwitcher.exe autoswitch 192.168.250.81 --mappings 1301/-255=1 1301/-256=2 1302/-65280=3

# Credits

Big thanks to Takumin 's post at https://note.com/taku_min/n/n985beda711f4 which pointed me in the right direction on a few things.