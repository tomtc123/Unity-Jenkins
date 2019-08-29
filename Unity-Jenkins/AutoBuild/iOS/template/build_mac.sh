#!/bin/sh
echo "start"
export untiy=/Applications/Unity5.5.6f1/Unity.app/Contents/MacOS/Unity
export projectPath=/Users/tomtang/Unity-Jenkins
 
echo "version = $version"
 
$untiy -quit -batchmode -projectPath $projectPath -logFile /Users/tomtang/Unity-Jenkins/build.log  -executeMethod JenkinsBuild.Build "version=$version"