$version = "0.0.1-beta"
$project = "QobuzDiscordBot.sln"
$runtimeIdentifiers = @("win-x64", "linux-x64", "osx-x64")

foreach ($rid in $runtimeIdentifiers) {
    $outputFolder = "./publish/$version/${rid}_${version}/"
    dotnet publish $project `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -c Release `
        -r $rid `
        -o $outputFolder

    if (Test-Path "$outputFolder/.env.example") {
        mv "$outputFolder/.env.example" "$outputFolder/.env"
    }
}
