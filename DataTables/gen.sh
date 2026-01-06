#!/bin/bash

WORKSPACE=/Users/wangshaowen/wgame
LUBAN_DLL=$WORKSPACE/Tools/Luban/Luban.dll
CONF_ROOT=/Users/wangshaowen/wgame/DataTables

dotnet $LUBAN_DLL \
    -t client \
    -c cs-bin \
    -d bin \
    -d json \
    --conf $CONF_ROOT/luban.conf \
    -x cs-bin.outputDataDir=$WORKSPACE/Assets/Res/LubanData/Bin \
    -x json.outputDataDir=$WORKSPACE/Assets/Res/LubanData/Json \
    -x outputCodeDir=$WORKSPACE/Assets/Scripts/Game/DataTable \