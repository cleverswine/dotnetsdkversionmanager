#!/usr/bin/env bash

# build a binary to ./dist for local testing

set -e

RID="osx-arm64"
if [ -n "$1" ]; then
	RID="$1"
fi
echo "building for ${RID}"

dotnet publish src/ --configuration Release --self-contained true -r "${RID}" -p:PublishSingleFile=true -o ./dist/"${RID}"
