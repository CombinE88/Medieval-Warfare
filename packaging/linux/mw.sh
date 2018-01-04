#!/bin/bash

command -v git >/dev/null 2>&1 || { echo >&2 "Compiling MW requires git."; exit 1; }
command -v make >/dev/null 2>&1 || { echo >&2 "Compiling MW requires make."; exit 1; }
command -v curl >/dev/null 2>&1 || { echo >&2 "Compiling MW requires curl."; exit 1; }
#echo "Cleaning"
#rm -rf Medieval-Warfare
#echo "Cloning MW"
#git clone https://github.com/CombinE88/Medieval-Warfare/
#echo "Done"
cd Medieval-Warfare
echo "Fetching engine..."
./fetch-engine.sh
echo "Fetched engine"
cd engine
echo "Getting dependencies..."
make linux-dependencies
echo "Done"
echo "Building engine and logic..."
make all
cd ..
make build
echo "Done"
echo "Do you want to start the game? (y/N)"
echo ""
read yn
case $yn in
    [Yy]* ) sh launch-game.sh; exit;;
    [Nn]* ) echo "Quiting"; exit;;
    * ) echo "Quiting"; exit;;
esac

