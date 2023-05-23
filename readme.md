# SC2BotQuick

Add buffering for SC2API robots. Improve performance on non-local networks.

Run on the server side.

## Build

```
dotnet publish SC2BotQuick/SC2BotQuick.csproj -c Release -o Release
```

## Run

```
cd Release
dotnet SC2BotQuick.dll -s 4567 -c 7890 -d 3 -r 2
```

```
s Server port.
c Client port.
d Delay.
r Resolution. Reduce bandwidth consumption.
```

The default StarCraft 2 game runs 22.4 loops per second.
