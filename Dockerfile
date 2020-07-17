FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY BridgeBotNext/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY BridgeBotNext/ ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim
RUN apt-get update && apt-get install -y libfontconfig1

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "BridgeBotNext.dll"]

RUN mkdir /data

#ENV BOT_VK__ACCESSTOKEN abc
#ENV BOT_VK__GROUPID
#ENV BOT_TG__BOTTOKEN
ENV BOT_AUTH__ENABLED false
#ENV BOT_AUTH__PASSWORD

ENV BOT_DBPROVIDER sqlite
ENV ConnectionStrings__sqlite "Data Source=/data/bridgebotnext.db"

VOLUME /data

ARG VCS_REF

LABEL org.label-schema.vcs-ref=$VCS_REF \
	  org.label-schema.vcs-url="https://github.com/maksimkurb/BridgeBotNext"