## Host Machine
Took ~2.5 hours to compile & packing a portable version

```
OS: Debian GNU/Linux 12 (bookworm) aarch64 
OS Mod: Twister OS version 3.1.0 
Host: Raspberry Pi 5 Model B Rev 1.1 
Kernel: 6.12.34+rpt-rpi-v8 
Uptime: 1 hour, 3 mins 
Packages: 3339 (dpkg), 5 (flatpak) 
Shell: bash 5.2.15 
Resolution: 1920x1080 @ 120.00Hz 
DE: Xfce 4.18 
WM: Xfwm4 
WM Theme: Breeze-Dark 
Theme: Breeze-Dark [GTK2], Adwaita [GTK3] 
Icons: breeze-dark [GTK2], Adwaita [GTK3] 
Terminal: xfce4-terminal 
Terminal Font: Monospace 12 
CPU: Cortex-A76 (4) @ 2.800GHz [51.8°C] 
GPU: V3D 7.1 
Memory: 2061MiB / 7952MiB 
```

## Dependencies
`sudo apt install -y qttools5-dev chrpath tcl-dev qttools5-dev-tools libreadline-dev qtdeclarative5-dev`
Available `libsqlite3-dev` is very old & unsupported
### Compile `sqlite3.41.x`
```
mkdir ~/.local
cd ~/.local
wget https://sqlite.org/tmp/tcl9.0.0.tar.gz
tar -xvf tcl9.0.0.tar.gz
cd tcl9.0.0/unix
./configure --prefix=$HOME/.local
make install
cd ../..
wget https://sqlite.org/2025/sqlite-src-3500400.zip
unzip sqlite-src-3500400.zip
cd sqlite-src-3500400
./configure --enable-all --with-tclsh=$HOME/local/bin/tclsh9.0 --prefix=$HOME/.local
make install
sudo nano /etc/ld.so.conf.d/aarch64-linux-gnu.conf
#add this line (replace user) ->  /home/ukhan/.local/lib
sudo ldconfig -v
nano ~/.bashrc
#EOF add this line ->
export PATH=$PATH:$HOME/dotnet:/home/ukhan/.local/bin
```

## Build Instructions
[Official Link](https://github.com/pawelsalawa/sqlitestudio/wiki/Instructions_for_compilation_under_Linux)
> Automatic compilation (recommended)

## Ease of Access
Download & place `launch.sh` in the SQLiteStudio folder. Do following to launch application from anywhere via terminal or make a desktop shortcut
```
chmod +x launch.sh
sudo ln -s <full-path>/launch.sh /usr/local/bin/sqlitestudio # use pwd to get full absolute path
nano /usr/local/share/applications/sqlitestudio.desktop
```
contents of `.desktop` file; change `Icon` location
```
[Desktop Entry]
Name=SQLite Studio
Comment=Studio for SQLite is a light GUI editor for SQLite databases
Exec=sqlitestudio
Icon=<full-path>/sqlitestudio.png
Terminal=false
X-MultipleArgs=false
Type=Application
Categories=Development;Utility;Database;
MimeType=application/vnd.db4s-project+xml;application/sqlitestudio;application/x-sqlitestudio;application/vnd.sqlite3;application/geopackage+sqlite3;application/x-sqlite2;application/x-sqlite3;
```
