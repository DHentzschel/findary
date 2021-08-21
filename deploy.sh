#!/bin/bash

project_name="findary"
dotnet="net5.0"
if [[ $# -ne 2 ]]; then
  echo "Arguments missing deploy.sh <version> <architecture>"
  exit
fi

dir="Findary/bin"
release_dir="../"

if [[ $2 == "386" ]]; then
  dir="${dir}/x86/Release"
elif [[ $2 == "amd64" ]]; then
  dir="${dir}/Release"
  release_dir=""
elif [[ $2 == "arm32" ]]; then
  dir="${dir}/ARM32/Release"
elif [[ $2 == "arm64" ]]; then
  dir="${dir}/ARM64/Release"
fi

cd $dir

zip_file_name="${project_name}-$1-dotnet5-windows-$2"
cp -r $dotnet $zip_file_name

zip -rv $release_dir../$zip_file_name.zip $zip_file_name/*.dll $zip_file_name/*.exe $zip_file_name/findary.runtimeconfig.json $zip_file_name/findary.deps.json

rm -rf $zip_file_name
