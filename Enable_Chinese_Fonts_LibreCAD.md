# Install-Chinese-Input-on-Raspberry-Pi-Raspbian

This repo records the steps to install Chinese input Google Pinyin on the Raspberry Pi Raspbian system.

## 1. Add Chinese locale ('zh_CN.UTF-8') via:
```
sudo dpkg-reconfigure locales
```
Select and install: zh_CN.UTF-8 UTF-8

## 2. Install Chinese fonts:
```
sudo apt-get install \
fonts-arphic-bkai00mp \
fonts-arphic-bsmi00lp \
fonts-arphic-gbsn00lp \
fonts-arphic-gkai00mp \
xfonts-intl-chinese \
xfonts-intl-chinese-big
```
**These steps are enough for LibreCAD font display**

## 3. Install fcitx and fcitx-libpinyin:
```
sudo apt-get install \
fcitx \
fcitx-pinyin
fcitx-googlepinyin
```
## 4. Open the Input Method Configuration

Either from the command line:
```
sudo im-config
```
or via the main menu:
```
Raspberry Pi Menu > Preferences > Input Method
```
- First screen:
Current confituration for the input method...
click [OK]

- Second screen:
Do you explicetly select the user configuration?
click [YES]

- Third screen:
Select: (*) fcitx activate Flexible Input Method Framework (fcitx) @
click [OK]

## 5. Reboot

There should be a small keyboard icon on the right side of the panel.

- right click on the keyboard icon
and choose either 'Configure current input method' or 'Configure'
- click the '+' symbol on the lower left of the window popping up
- Uncheck 'Only Show Current Language' in the window popping up
- Enter the term 'pinyin' in the search field
- select the field 'GooglePinyin' and click [OK]
- close the window

Now it should be possible to select the input method
'GooglePinyin' from the keyboard icon in the panel:

- right click on the keyboard icon
- hover over 'input method'
- and select 'GooglePinyin'

The input method now can be toggled between English and Chinese using
the key combination 'ctrl-space'...

## Refrences
- https://www.raspberrypi.org/forums/viewtopic.php?t=222801
- https://hhddkk.wordpress.com/2016/06/02/install-google-pinyin-in-ubuntu-16-04/
