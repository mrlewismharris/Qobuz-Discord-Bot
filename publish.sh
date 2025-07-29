#!/bin/bash

version="0.0.1-beta"
project="QobuzDiscordBot.sln"
rids=("win-x64" "linux-x64" "osx-x64")

for rid in "${rids[@]}"; do
    output_folder="./publish/$version/${rid}_${version}/"
    dotnet publish "$project" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:DebugType=none \
        -p:DebugSymbols=false \
        -c Release \
        -r "$rid" \
        -o "$output_folder"
    mv "$output_folder/.env.example" "$output_folder/.env"
done
