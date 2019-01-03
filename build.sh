#!/usr/bin/env bash

set -eu

cd "$(dirname "$0")"

FAKE_VERSION=5.10.1
TOOL_PATH=$(pwd)/tools

if [ ! -e "$TOOL_PATH" ]; then
  mkdir $TOOL_PATH
fi

## Install the FAKE tool if it's not installed already and put it in the `./tools` directory.
TOOLS_INSTALLED=$(dotnet tool list --tool-path $TOOL_PATH)
if echo $TOOLS_INSTALLED | grep "fake-cli" | grep -q "$FAKE_VERSION" ; then
  echo "FAKE $FAKE_VERSION already installed."
else
  if echo $TOOLS_INSTALLED | grep -q "fake-cli" ; then 
    echo "Uninstalling existing FAKE cli tool"
    sudo dotnet tool uninstall fake-cli --tool-path $TOOL_PATH
  fi
  echo "Installing FAKE cli tool"
  dotnet tool install fake-cli --tool-path $TOOL_PATH --version $FAKE_VERSION
fi

$TOOL_PATH/fake build "$@"

