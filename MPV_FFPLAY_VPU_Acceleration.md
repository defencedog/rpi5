# MPV not as fast as VLC
Custom compiling `mpv` & `ffmpeg` as these do not provide hardware accelerated playback by default
Compiling is resource intensive; if you are using inadequate power supply compiler can produce _Segmentation fault_ errors

## Enable source packages
`sudo apt build-dep mpv ffmpeg` You may need to edit _/etc/apt/sources.list_ & uncomment lines starting with `deb-src`

## Enable `aac` support
```
git clone https://github.com/mstorsjo/fdk-aac && \
cd fdk-aac && \
autoreconf -fiv && \
./configure --enable-shared && \
make -j2 && \
sudo make install && sudo ldconfig
```
## Compile `ffmpeg`
```
git clone -b release/5.1/main https://github.com/jc-kynesim/rpi-ffmpeg.git && cd rpi-ffmpeg && ./configure --prefix=/usr/local --toolchain=hardened --incdir=/usr/include/aarch64-linux-gnu --enable-gpl --disable-stripping --disable-mmal --enable-gnutls --enable-ladspa --enable-libaom --enable-libass --enable-libbluray --enable-libbs2b --enable-libcaca --enable-libcdio --enable-libcodec2 --enable-libdav1d --enable-libflite --enable-libfontconfig --enable-libfreetype --enable-libfribidi --enable-libglslang --enable-libgme --enable-libgsm --enable-libjack --enable-libmp3lame --enable-libmysofa --enable-libopenjpeg --enable-libopenmpt --enable-libopus --enable-libpulse --enable-librabbitmq --enable-librist --enable-librubberband --enable-libshine --enable-libsnappy --enable-libsoxr --enable-libspeex --enable-libsrt --enable-libssh --enable-libsvtav1 --enable-libtheora --enable-libtwolame --enable-libvidstab --enable-libvorbis --enable-libvpx --enable-libwebp --enable-libx265 --enable-libxml2 --enable-libxvid --enable-libzimg --enable-libzmq --enable-libzvbi --enable-lv2 --enable-omx --enable-openal --enable-opencl --enable-opengl --enable-sand --enable-sdl2 --disable-sndio --enable-libjxl --enable-neon --enable-v4l2-request --enable-libudev --enable-epoxy --libdir=/usr/lib/aarch64-linux-gnu --arch=arm64 --enable-pocketsphinx --enable-librsvg --enable-libdc1394 --enable-libdrm --enable-vout-drm --enable-libiec61883 --enable-chromaprint --enable-frei0r --enable-libx264 --enable-libplacebo --enable-librav1e --enable-shared --disable-htmlpages --disable-manpages --disable-podpages --disable-txtpages --enable-nonfree --enable-libfdk-aac --disable-static --disable-cuda --enable-vaapi --disable-vdpau --enable-vout-drm --enable-vout-egl --enable-vulkan --disable-nvdec --disable-nvenc --disable-cuvid --disable-cuda-llvm --disable-cuda-nvcc --disable-ffnvcodec && make -j2 && sudo make -j2 install
```
if cloning takes too much time you can download `tar.gz` archive
> https://github.com/jc-kynesim/rpi-ffmpeg/archive/refs/tags/pi/5.1.6/rpi_26.tar.gz

## Compile `mpv`
```
sudo apt source mpv
cd <path to mpv> # change as required
meson setup build
meson configure build -Dprefix=/usr/local -Dlibmpv=true -Ddvdnav=enabled -Dsdl2=enabled -Dzimg=enabled -Drpi=disabled -Drpi-mmal=disabled -Degl=enabled -Dwayland=enabled -Degl-wayland=enabled -Dsdl2=enabled -Dvaapi=disabled -Dvdpau=disabled -Dvulkan=enabled -Dcuda-hwaccel=disabled -Dcuda-interop=disabled -Ddvdnav=enabled -Dzlib=enabled -Dzimg=enabled -Dvdpau=disabled -Dvaapi=enabled
sudo meson install -C build
```

## Cleaning up &  saving space
This installs all to `/usr/local/lib` and `/usr/local/include` `/usr/local/bin` so you can delete source / compile files & folders

## Configuring `mpv`
_~/.config/mpv/mpv.conf_
```
gpu-dumb-mode=yes
opengl-glfinish=yes
gpu-context=wayland #remove if using X.org
gpu-api=opengl
vo=gpu
gpu-sw=yes
drm-vrr-enabled=auto
hwdec=drm-copy
```
Then disable install/update of custom compiled packages bu `sudo apt-mark hold ffmpeg mpv`. 
You can also use `mpv` another GUI via `sudo apt install celluloid`. In this program preferences point to _mpv.conf_ file
