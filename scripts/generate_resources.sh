#!/bin/bash

# create resources folder if it doesn't exist already
if [ ! -d "src/Resources" ] && [ ! -L "src/Resources" ]
then
    mkdir -p "src/Resources"
fi

if [ $# -eq 0 ]
then   
    printf "debug" > src/Resources/type.txt
else
    printf "$1" > src/Resources/type.txt
fi

build=`date`
echo $build > src/Resources/buildtime.txt