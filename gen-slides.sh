#!/bin/sh
if [ ! -d FsReveal ]
then
    git clone --depth 50 https://github.com/fsprojects/FsReveal.git
fi
cp -r ./slides/* ./FsReveal/slides/
cp ./build.fsx.copy ./FsReveal/build.fsx
if [ -f ./FsReveal/slides/sample.fsx ]
then
    rm ./FsReveal/slides/sample.fsx
fi
if [ -f ./FsReveal/slides/images/logo.png ]
then
    rm ./FsReveal/slides/images/logo.png
fi
cd ./FsReveal
./build.sh ReleaseSlides
cd ..
